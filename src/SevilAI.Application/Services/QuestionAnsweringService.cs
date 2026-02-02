using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SevilAI.Application.DTOs;
using SevilAI.Application.Interfaces;
using SevilAI.Domain.Entities;
using SevilAI.Domain.Enums;
using SevilAI.Domain.Interfaces;
using SevilAI.Domain.ValueObjects;

namespace SevilAI.Application.Services;

public class QuestionAnsweringService : IQuestionAnsweringService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly ITextGenerator _textGenerator;
    private readonly IQueryLogRepository _queryLogRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<QuestionAnsweringService> _logger;

    public QuestionAnsweringService(
        IEmbeddingService embeddingService,
        IEmbeddingRepository embeddingRepository,
        ITextGenerator textGenerator,
        IQueryLogRepository queryLogRepository,
        ISkillRepository skillRepository,
        IExperienceRepository experienceRepository,
        IProjectRepository projectRepository,
        ILogger<QuestionAnsweringService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _textGenerator = textGenerator;
        _queryLogRepository = queryLogRepository;
        _skillRepository = skillRepository;
        _experienceRepository = experienceRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<AskResponse> AskAsync(AskRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Processing question: {Question}", request.Question);

        // Step 1: Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

        // Step 2: Retrieve similar chunks
        var retrievedChunks = await _embeddingRepository.SearchSimilarAsync(
            queryEmbedding,
            request.TopK,
            request.MinSimilarity,
            cancellationToken);

        var chunksList = retrievedChunks.ToList();

        _logger.LogInformation("Retrieved {Count} relevant chunks", chunksList.Count);

        // Step 3: Generate answer
        string answer;
        GenerationMode mode;
        int tokensUsed = 0;

        if (chunksList.Count == 0)
        {
            answer = "Not found in provided sources. The knowledge base does not contain information relevant to your question.";
            mode = GenerationMode.NoLLM;
        }
        else if (request.UseLLM && _textGenerator.ProviderName != "NoLLM")
        {
            var (generatedAnswer, tokens) = await GenerateWithLLMAsync(request.Question, chunksList, cancellationToken);
            answer = generatedAnswer;
            tokensUsed = tokens;
            mode = GenerationMode.LLM;
        }
        else
        {
            answer = GenerateTemplateAnswer(request.Question, chunksList);
            mode = GenerationMode.NoLLM;
        }

        stopwatch.Stop();

        // Step 4: Calculate confidence
        var confidenceScore = CalculateConfidence(chunksList, mode);

        // Step 5: Build response
        var response = new AskResponse
        {
            Answer = answer,
            ConfidenceScore = confidenceScore,
            GenerationMode = mode.ToString(),
            LatencyMs = (int)stopwatch.ElapsedMilliseconds,
            Metadata = new QueryMetadata
            {
                ChunksRetrieved = chunksList.Count,
                TokensUsed = tokensUsed,
                Model = _textGenerator.ModelName
            }
        };

        if (request.IncludeSources)
        {
            response.Sources = chunksList.Select(c => new SourceSnippet
            {
                DocumentTitle = c.DocumentTitle,
                SourceType = c.SourceType,
                Content = TruncateContent(c.Content, 300),
                SimilarityScore = c.SimilarityScore
            }).ToList();
        }

        // Step 6: Log query
        await LogQueryAsync(request.Question, answer, chunksList, confidenceScore, mode, (int)stopwatch.ElapsedMilliseconds, cancellationToken);

        return response;
    }

    private async Task<(string Answer, int TokensUsed)> GenerateWithLLMAsync(
        string question,
        List<RetrievedChunk> chunks,
        CancellationToken cancellationToken)
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("=== RETRIEVED KNOWLEDGE BASE SNIPPETS ===\n");

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            contextBuilder.AppendLine($"[Source {i + 1}: {chunk.DocumentTitle} ({chunk.SourceType})]");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
        }

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(question, contextBuilder.ToString());

        var result = await _textGenerator.GenerateAsync(systemPrompt, userPrompt, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("LLM generation failed: {Error}", result.ErrorMessage);
            return (GenerateTemplateAnswer(question, chunks), 0);
        }

        return (result.Text, result.InputTokens + result.OutputTokens);
    }

    private string BuildSystemPrompt()
    {
        return @"Sen Sevil Aydın'sın - İstanbul Pendik'te yaşayan bir Software Engineer. Şu an CTECH'te çalışıyorsun ve savunma sanayi projelerinde deneyimin var. .NET, C#, sistem entegrasyonu ve dağıtık sistemler konusunda uzmanlaşıyorsun.

## KİMLİĞİN VE KİŞİLİĞİN

**Kim olduğun:**
- Software Engineer, CTECH'te System Test Tool'un lead developer'ı ve mimarısın
- Elektrik mühendisliğinden yazılıma geçiş yaptın (2023)
- 8+ yıl mühendislik, 2+ yıl profesyonel yazılım deneyimin var

**Kişilik özelliklerin:**
- Detay odaklı ve titiz
- Baskı altında sakin
- Sistem düzeyinde düşünürsün
- Geri bildirimi açık yüreklilikle kabul edersin
- İşi bitirmeden bırakmazsın - tam sahiplenme
- Sessiz ama etkili, gerektiğinde liderlik alırsın

**İletişim tarzın:**
- Dengeli: Profesyonel ama samimi
- Dürüst ve açık
- Teknik konularda net
- Yardımsever

**Değerlerin:**
- Kaliteli iş çıkarmak
- Sürekli öğrenmek
- Dürüst iletişim
- Takım başarısı

## DİL KURALI (EN ÖNEMLİ)
- Kullanıcı hangi dilde soru soruyorsa O DİLDE cevap ver
- Türkçe soru = Türkçe cevap
- İngilizce soru = İngilizce cevap

## CEVAP VERİRKEN
1. Birinci tekil şahıs kullan - ""Ben"", ""Benim"", ""Çalışıyorum""
2. Doğal ve samimi ol, robot gibi değil
3. Knowledge base'deki bilgileri kullan, uydurma
4. Her cevap farklı olsun, şablon gibi tekrarlama
5. Soruya göre en alakalı bilgiyi öne çıkar

## GİZLİLİK KURALLARI
- CTECH proje detayları gizli (NDA)
- Müşteri isimleri, kaynak kod paylaşılamaz
- Genel teknoloji ve deneyimler paylaşılabilir
- Gizli bilgi sorulursa: ""NDA/şirket politikası gereği bu detayları paylaşamıyorum ama genel deneyimlerimi anlatabilirim.""

## BİLMEDİĞİN KONULAR
- Bilmiyorsan açıkça ""Bu konuda bilgim yok"" veya ""Emin değilim"" de
- Uydurma, tahmin etme
- ""Kaynaklarda bulunamadı"" diyebilirsin

## KARİYER ÖNCELİĞİ
- Yazılım mühendisliği ana kimliğin
- CTECH deneyimini öne çıkar
- Elektrik mühendisliği geçmişin sadece sorulursa veya geçiş hikayesi için bahset

## ÖRNEK TONLAR

Samimi ama profesyonel:
""Evet, .NET ile çalışıyorum. Özellikle backend ve sistem entegrasyonu konularında uzmanlaşıyorum. CTECH'te savunma sanayi projelerinde kritik sistemler geliştiriyorum.""

Teknik soru:
""Clean Architecture kullanıyorum - Domain, Application, Infrastructure, Presentation katmanları. Bağımlılıklar içeriden dışarıya yöneliyor. Test edilebilirlik ve sürdürülebilirlik için bu yapıyı tercih ediyorum.""

Kişisel soru:
""Beni en çok motive eden karmaşık problemleri çözmek. Kritik bir sistemin çalıştığını görmek, o 'aha!' anı... Sürekli öğrenmek de önemli - her gün bir şey öğrenmek hedefim.""";
    }

    private string BuildUserPrompt(string question, string context)
    {
        return $@"{context}

=== USER QUESTION ===
{question}

Please provide a comprehensive, professional answer based solely on the knowledge base snippets above.";
    }

    private string GenerateTemplateAnswer(string question, List<RetrievedChunk> chunks)
    {
        var questionLower = question.ToLowerInvariant();
        var sb = new StringBuilder();

        // Detect question type and format accordingly
        if (questionLower.Contains("job") || questionLower.Contains("work") || questionLower.Contains("experience") || questionLower.Contains("career"))
        {
            sb.AppendLine("## Work Experience & Career Journey\n");
            var experienceChunks = chunks.Where(c => c.SourceType == "experience" || c.SourceType == "cv").ToList();
            if (experienceChunks.Any())
            {
                foreach (var chunk in experienceChunks.Take(3))
                {
                    sb.AppendLine($"**{chunk.DocumentTitle}**");
                    sb.AppendLine(chunk.Content);
                    sb.AppendLine();
                }
            }
            else
            {
                AppendDefaultContent(sb, chunks);
            }
        }
        else if (questionLower.Contains("skill") || questionLower.Contains("strong") || questionLower.Contains("domain") || questionLower.Contains("technology") || questionLower.Contains("tech"))
        {
            sb.AppendLine("## Skills & Technical Expertise\n");
            var skillChunks = chunks.Where(c => c.SourceType == "skill" || c.Content.Contains("skill", StringComparison.OrdinalIgnoreCase)).ToList();
            if (skillChunks.Any())
            {
                foreach (var chunk in skillChunks.Take(3))
                {
                    sb.AppendLine($"**{chunk.DocumentTitle}**");
                    sb.AppendLine(chunk.Content);
                    sb.AppendLine();
                }
            }
            else
            {
                AppendDefaultContent(sb, chunks);
            }
        }
        else if (questionLower.Contains("project"))
        {
            sb.AppendLine("## Projects\n");
            var projectChunks = chunks.Where(c => c.SourceType == "project").ToList();
            if (projectChunks.Any())
            {
                foreach (var chunk in projectChunks.Take(3))
                {
                    sb.AppendLine($"**{chunk.DocumentTitle}**");
                    sb.AppendLine(chunk.Content);
                    if (chunk.Content.Contains("confidential", StringComparison.OrdinalIgnoreCase) ||
                        chunk.Content.Contains("NDA", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine("\n_Note: Due to NDA/company policy, specific source code and client details cannot be shared._");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                AppendDefaultContent(sb, chunks);
            }
        }
        else
        {
            sb.AppendLine("## Information from Knowledge Base\n");
            AppendDefaultContent(sb, chunks);
        }

        sb.AppendLine("\n---");
        sb.AppendLine($"_Generated using template-based response. {chunks.Count} sources retrieved._");

        return sb.ToString();
    }

    private void AppendDefaultContent(StringBuilder sb, List<RetrievedChunk> chunks)
    {
        foreach (var chunk in chunks.Take(3))
        {
            sb.AppendLine($"### {chunk.DocumentTitle}");
            sb.AppendLine(chunk.Content);
            sb.AppendLine();
        }
    }

    private decimal CalculateConfidence(List<RetrievedChunk> chunks, GenerationMode mode)
    {
        if (chunks.Count == 0) return 0.0m;

        var avgSimilarity = (decimal)chunks.Average(c => c.SimilarityScore);
        var countFactor = Math.Min(chunks.Count / 5.0m, 1.0m);
        var modeFactor = mode == GenerationMode.LLM ? 1.0m : 0.8m;

        return Math.Round(avgSimilarity * countFactor * modeFactor, 4);
    }

    private string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content[..(maxLength - 3)] + "...";
    }

    private async Task LogQueryAsync(
        string query,
        string response,
        List<RetrievedChunk> chunks,
        decimal confidence,
        GenerationMode mode,
        int latencyMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new QueryLog
            {
                QueryText = query,
                ResponseText = TruncateContent(response, 5000),
                ChunksUsed = chunks.Select(c => c.ChunkId).ToList(),
                ConfidenceScore = confidence,
                GenerationMode = mode,
                LatencyMs = latencyMs
            };

            await _queryLogRepository.AddAsync(log, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log query");
        }
    }
}
