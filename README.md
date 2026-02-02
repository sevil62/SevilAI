# SevilAI - RAG-Based Knowledge Engine

A portfolio-grade, production-ready knowledge engine that answers questions about Sevil Aydın's professional profile using Retrieval-Augmented Generation (RAG).

## Overview

SevilAI is an AI-powered knowledge assistant that:
- Answers questions about skills, experience, projects, and career goals
- Estimates project effort based on skill profile and engineering heuristics
- Provides well-structured, professional responses with source citations
- **Never hallucinates** - only uses data from the seeded knowledge base

## Architecture

```
SevilAI/
├── src/
│   ├── SevilAI.Api/           # ASP.NET Core Web API
│   ├── SevilAI.Application/   # Business logic, services, DTOs
│   ├── SevilAI.Domain/        # Entities, interfaces, value objects
│   └── SevilAI.Infrastructure/# Repositories, embeddings, LLM providers
├── tests/
│   └── SevilAI.Tests/         # Unit tests (xUnit + FluentAssertions)
└── docker/
    ├── docker-compose.yml     # PostgreSQL + API orchestration
    ├── Dockerfile            # Multi-stage build
    └── init-scripts/         # Database schema
```

### Clean Architecture

The project follows Clean Architecture principles:
- **Domain Layer**: Core entities (Document, Chunk, Embedding, Skill, Experience, Project)
- **Application Layer**: Use cases, DTOs, service interfaces
- **Infrastructure Layer**: PostgreSQL repositories, embedding service, LLM providers
- **API Layer**: REST endpoints, configuration, middleware

## Features

### 1. Question Answering (RAG)

```http
POST /api/ask
{
  "question": "What jobs did Sevil do?",
  "topK": 5,
  "minSimilarity": 0.3,
  "useLLM": true,
  "includeSources": true
}
```

Response:
```json
{
  "answer": "Sevil has worked in two main roles...",
  "confidenceScore": 0.85,
  "generationMode": "LLM",
  "sources": [
    {
      "documentTitle": "CTECH Experience",
      "sourceType": "experience",
      "content": "Software Engineer at CTECH...",
      "similarityScore": 0.92
    }
  ],
  "latencyMs": 450
}
```

### 2. Effort Estimation

```http
POST /api/estimate
{
  "projectDescription": "Build a REST API with authentication",
  "requiredFeatures": ["authentication", "CRUD operations", "logging"],
  "techStack": [".NET 8", "PostgreSQL", "Docker"],
  "constraints": ["security compliance"],
  "detailedBreakdown": true
}
```

Response includes:
- Day-by-day estimates (min/max/recommended)
- Phase breakdown (Planning, Development, Testing, Deployment)
- Assumptions and risks
- Recommended technologies
- Confidence score

### 3. Answer Generation Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| **LLM** | Natural language generation using retrieved context | Production with API key |
| **NoLLM** | Template-based response builder | Free/offline usage |

The system enforces grounding rules:
- Uses **only** retrieved snippets
- Says "Not found in provided sources" when data is missing
- Includes confidence score and source citations

## Technology Stack

- **Backend**: .NET 8, ASP.NET Core Web API
- **Database**: PostgreSQL 16 with pgvector extension
- **Vector Search**: Cosine similarity on 384-dimensional embeddings
- **LLM Providers** (optional):
  - Groq (free tier available)
  - OpenRouter (free models available)
  - Google Gemini (free tier available)

## Getting Started

### Prerequisites

- Docker & Docker Compose
- .NET 8 SDK (for local development)

### Quick Start with Docker

1. **Clone and navigate**
   ```bash
   cd SevilAI
   ```

2. **Start the services**
   ```bash
   cd docker
   docker-compose up -d
   ```

3. **Seed the knowledge base**
   ```bash
   curl -X POST http://localhost:5000/api/seed/default
   ```

4. **Ask a question**
   ```bash
   curl -X POST http://localhost:5000/api/ask \
     -H "Content-Type: application/json" \
     -d '{"question": "What technologies does Sevil work with?"}'
   ```

5. **Open Swagger UI**
   Navigate to http://localhost:5000

### Local Development

1. **Start PostgreSQL**
   ```bash
   cd docker
   docker-compose up sevilai-db -d
   ```

2. **Configure connection**
   Update `appsettings.Development.json` if needed.

3. **Run the API**
   ```bash
   cd src/SevilAI.Api
   dotnet run
   ```

4. **Run tests**
   ```bash
   cd tests/SevilAI.Tests
   dotnet test
   ```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | localhost |
| `LLMSettings__Provider` | LLM provider (groq/openrouter/gemini/none) | none |
| `LLMSettings__ApiKey` | API key for LLM provider | - |
| `EmbeddingSettings__Dimensions` | Embedding vector dimensions | 384 |

### LLM Provider Setup

#### Groq (Recommended - Free)
```json
{
  "LLMSettings": {
    "Provider": "groq",
    "Groq": {
      "ApiKey": "your-groq-api-key",
      "Model": "llama-3.3-70b-versatile"
    }
  }
}
```

#### OpenRouter
```json
{
  "LLMSettings": {
    "Provider": "openrouter",
    "OpenRouter": {
      "ApiKey": "your-openrouter-api-key",
      "Model": "meta-llama/llama-3.3-70b-instruct:free"
    }
  }
}
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/ask` | POST | Ask a question about the knowledge base |
| `/api/ask/examples` | GET | Get example questions |
| `/api/estimate` | POST | Estimate project effort |
| `/api/estimate/guidelines` | GET | Get estimation guidelines |
| `/api/seed` | POST | Seed knowledge base with JSON |
| `/api/seed/default` | POST | Seed from embedded resource |
| `/api/health` | GET | System health and stats |
| `/api/health/live` | GET | Liveness probe |
| `/api/health/ready` | GET | Readiness probe |

## Seeding Custom Data

You can seed your own knowledge base by POSTing JSON to `/api/seed`:

```json
{
  "jsonData": "{\"person\": {...}, \"careerJourney\": [...]}",
  "clearExisting": true
}
```

The schema supports:
- `person` - Name, title, location, focus areas, career goals
- `character` - Work ethic, traits, team style
- `careerJourney` - Work experiences with impact and contributions
- `enterpriseProject` - Confidential project details
- `personalProjects` - Portfolio projects
- `effortProfile` - Productivity characteristics

## NDA & Confidentiality

This system is designed for professional use with NDA considerations:

- **No source code** from confidential projects is stored
- Only **sanitized summaries** of architecture and responsibilities
- When discussing confidential work, responses include:
  > "Due to NDA/company policy, specific source code and client details cannot be shared."

## Testing

The project includes comprehensive tests:

```bash
dotnet test --verbosity normal
```

Test coverage includes:
- **ChunkingServiceTests** - Text chunking and tokenization
- **EffortEstimationServiceTests** - Estimation accuracy and factors
- **LocalEmbeddingServiceTests** - Embedding generation and similarity
- **QuestionAnsweringServiceTests** - RAG pipeline and response formatting
- **PromptSafetyTests** - Injection prevention and grounding

## Database Schema

```sql
-- Core tables
documents       -- Source documents (CV, notes, etc.)
chunks          -- Document chunks for retrieval
embeddings      -- Vector embeddings (384 dimensions)
skills          -- Structured skill data
experiences     -- Work experience records
projects        -- Personal and enterprise projects
query_logs      -- Query analytics
```

Vector similarity search uses `pgvector` with IVFFlat index for efficient cosine distance queries.

## Performance Considerations

- **Embedding generation**: Local hash-based (fast, no API calls)
- **Vector search**: IVFFlat index for O(log n) similarity search
- **Chunking**: 500 tokens max with 50 token overlap
- **Caching**: Consider adding Redis for production

## Contributing

This is a portfolio project demonstrating:
- Clean Architecture in .NET
- RAG implementation with vector search
- Professional API design
- Comprehensive testing practices

## License

MIT License - See LICENSE file for details.

---

Built with .NET 8 | PostgreSQL | pgvector | Clean Architecture
