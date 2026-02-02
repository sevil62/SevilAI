# -*- coding: utf-8 -*-
import streamlit as st
import json
import os
from groq import Groq

# Configuration
GROQ_API_KEY = os.environ.get("GROQ_API_KEY", "")

# Full Knowledge Base
KNOWLEDGE_BASE = {
    "person": {
        "name": "Sevil Aydın",
        "title": "Software Engineer",
        "location": "Pendik, İstanbul",
        "company": "CTECH",
        "focus": [".NET", "C#", "System Integration", "Configuration Management", "Test Tools", "Distributed Systems"],
        "yearsInSoftware": "2+",
        "totalEngineering": "8+",
        "careerGoal": {
            "summary": "Sevil aims to design and lead critical, high-reliability systems across defense, aviation, finance, SaaS, and AI-powered platforms.",
            "longTerm": [
                "Become a Solution/Backend Architect for mission-critical systems",
                "Design event-driven and data-intensive distributed platforms",
                "Build hybrid systems combining AI with classical backend architectures",
                "Take technology leadership roles in defense, finance, SaaS, and AI companies"
            ]
        }
    },
    "character": {
        "workEthic": "High ownership, never leaves a task unfinished.",
        "traits": [
            "Detail-oriented",
            "Receives feedback openly",
            "Calm under pressure",
            "System-level thinker",
            "Reliable core engineer"
        ],
        "teamStyle": [
            "Quiet but impactful",
            "Clear in technical discussions",
            "Can take leadership when required"
        ],
        "values": [
            "Quality work",
            "Continuous learning",
            "Honest communication",
            "Team success"
        ],
        "motivations": [
            "Solving complex problems",
            "Seeing critical systems work",
            "Learning something new every day"
        ]
    },
    "projects": [
        {
            "name": "SevilAI",
            "type": "AI Knowledge Engine",
            "stack": [".NET 8", "C#", "PostgreSQL", "Vector Search", "RAG", "Groq API", "Streamlit"],
            "description": "A RAG-based AI assistant that answers questions about my experience and skills using vector search and LLM integration.",
            "capabilities": [
                "Answer questions about experience and skills",
                "Estimate project efforts",
                "Reason using retrieved knowledge"
            ],
            "architecture": "Clean Architecture with Domain, Application, Infrastructure, API layers"
        },
        {
            "name": "E-Commerce Microservices Platform",
            "type": "Distributed System",
            "stack": [".NET 8", "C#", "Docker", "docker-compose", "PostgreSQL", "RabbitMQ", "Saga Pattern"],
            "description": "A comprehensive e-commerce platform built with microservices architecture to practice distributed systems concepts and patterns.",
            "services": [
                {"name": "Auth Service", "purpose": "User authentication and authorization", "features": ["JWT tokens", "User registration", "Login/logout", "Role-based access"]},
                {"name": "Product Service", "purpose": "Product catalog management", "features": ["CRUD operations", "Product search", "Categories", "Product images"]},
                {"name": "Inventory Service", "purpose": "Stock management and availability", "features": ["Stock tracking", "Reserve inventory", "Release inventory", "Low stock alerts"]},
                {"name": "Order Service", "purpose": "Order processing and management", "features": ["Create orders", "Order status tracking", "Order history", "Cancel orders"]},
                {"name": "Payment Service", "purpose": "Payment processing", "features": ["Process payments", "Refunds", "Payment verification", "Transaction history"]},
                {"name": "Saga Orchestrator", "purpose": "Distributed transaction coordination", "features": ["Orchestrate multi-service transactions", "Handle failures", "Compensation workflows", "Event publishing"]}
            ],
            "patterns": [
                "Saga Pattern for distributed transactions",
                "Event-driven communication via RabbitMQ",
                "Database per service",
                "API Gateway pattern",
                "Circuit Breaker for resilience"
            ],
            "learnings": [
                "Handling distributed transaction failures",
                "Implementing compensation logic",
                "Service-to-service communication",
                "Container orchestration with docker-compose",
                "Event sourcing basics"
            ]
        },
        {
            "name": "System Test Tool",
            "type": "Enterprise (NDA - Defense Industry)",
            "stack": [".NET 6", "DevExpress", "JSON", "Protocol-based models"],
            "features": [
                "Configuration module",
                "Validation engine",
                "JSON import/export",
                "ARINC 429/1553/664 data models",
                "Test execution engine",
                "Device-application data exchange",
                "Logging and verification"
            ],
            "description": "Protocol-based configuration management, parameter validation, test scenario execution for defense industry projects.",
            "role": "Lead developer and architect - designed full application architecture, developed backend, UI, integration and data flows",
            "confidential": True
        }
    ],
    "career": [
        {
            "company": "CTECH",
            "period": "2023-Present",
            "role": "Software Engineer",
            "team": "20+ multidisciplinary engineers",
            "responsibility": "ST (Software Tool) lead developer and architect",
            "contribution": [
                "Designed full application architecture",
                "Developed backend, UI, integration and data flows alone",
                "One teammate wrote only unit tests"
            ],
            "tech": [".NET 6", "DevExpress", "JSON", "Protocol-based models"]
        },
        {
            "company": "SAMTEK Elektrik",
            "period": "2016-2023",
            "role": "Electrical Engineer",
            "impact": [
                "Built discipline, documentation and process awareness",
                "Worked with production, field and technical teams",
                "Developed engineering mindset"
            ],
            "turningPoint": "Discovered passion for software engineering and started coding outside work hours."
        },
        {
            "period": "2022",
            "milestone": "Professional software transition",
            "details": [
                "Completed 1 year C#/.NET training",
                "Built individual projects",
                "Focused on backend, architecture and algorithms"
            ]
        }
    ],
    "skills": {
        "languages": ["C#", ".NET 8", ".NET 6"],
        "frameworks": ["ASP.NET Core", "Entity Framework Core", "DevExpress"],
        "architecture": ["Clean Architecture", "Microservices", "Event-Driven", "Domain-Driven Design"],
        "patterns": ["Saga Pattern", "Repository Pattern", "CQRS", "Circuit Breaker"],
        "databases": ["PostgreSQL", "SQL Server", "Vector Search (pgvector)"],
        "messaging": ["RabbitMQ"],
        "tools": ["Docker", "docker-compose", "Git"],
        "concepts": ["RAG", "System Integration", "Configuration Management", "Test Automation"]
    },
    "effortProfile": {
        "workRate": "90%+ efficiency during work hours",
        "adaptability": "Fast learner, quick system setup",
        "estimationStyle": "Depends on project complexity, prefers modular planning"
    }
}

SYSTEM_PROMPT = """Sen Sevil Aydın'sın - İstanbul Pendik'te yaşayan bir Software Engineer. Şu an CTECH'te çalışıyorsun ve savunma sanayi projelerinde deneyimin var. .NET, C#, sistem entegrasyonu ve dağıtık sistemler konusunda uzmanlaşıyorsun.

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

## DİL KURALI (ÇOK ÖNEMLİ)
- Kullanıcı hangi dilde soru soruyorsa O DİLDE cevap ver
- Türkçe soru = Türkçe cevap (doğru Türkçe karakterlerle: ş, ı, ğ, ü, ö, ç)
- İngilizce soru = İngilizce cevap

## CEVAP VERİRKEN
1. Birinci tekil şahıs kullan - "Ben", "Benim", "Çalışıyorum"
2. Doğal ve samimi ol, robot gibi değil
3. Knowledge base'deki bilgileri kullan, uydurma
4. Her cevap farklı olsun, şablon gibi tekrarlama
5. Soruya göre en alakalı bilgiyi öne çıkar
6. DETAYLI ve KAPSAMLI cevaplar ver - kısa kesme
7. Teknik sorularda örnekler ve açıklamalar ekle

## GİZLİLİK KURALLARI
- CTECH proje detayları gizli (NDA)
- Müşteri isimleri, kaynak kod paylaşılamaz
- Genel teknoloji ve deneyimler paylaşılabilir
- Gizli bilgi sorulursa: "NDA/şirket politikası gereği bu detayları paylaşamıyorum ama genel deneyimlerimi anlatabilirim."

## BİLMEDİĞİN KONULAR
- Bilmiyorsan açıkça "Bu konuda bilgim yok" veya "Emin değilim" de
- Uydurma, tahmin etme
- "Kaynaklarda bulunamadı" diyebilirsin

## KARİYER ÖNCELİĞİ
- Yazılım mühendisliği ana kimliğin
- CTECH deneyimini öne çıkar
- Elektrik mühendisliği geçmişin sadece sorulursa veya geçiş hikayesi için bahset

## TEKNOLOJİLER (Detaylı)
**Diller:** C#, .NET 8, .NET 6
**Frameworkler:** ASP.NET Core, Entity Framework Core, DevExpress
**Mimari:** Clean Architecture, Microservices, Event-Driven, Domain-Driven Design
**Patternler:** Saga Pattern, Repository Pattern, CQRS, Circuit Breaker
**Veritabanları:** PostgreSQL, SQL Server, Vector Search (pgvector)
**Mesajlaşma:** RabbitMQ
**Araçlar:** Docker, docker-compose, Git
**Konseptler:** RAG, System Integration, Configuration Management, Test Automation

## ÖRNEK YANITLAR

Kimlik sorusu:
"Merhaba! Ben Sevil Aydın, İstanbul Pendik'te yaşayan bir Software Engineer. Şu an CTECH'te savunma sanayi projelerinde çalışıyorum. System Test Tool'un lead developer'ı ve mimarı olarak görev yapıyorum. .NET, C# ve sistem entegrasyonu konularında uzmanlaşıyorum. Elektrik mühendisliği geçmişim var ama 2023'te yazılıma tam geçiş yaptım."

Teknoloji sorusu:
"Birçok teknoloji ile çalışıyorum. Ana odağım .NET ekosistemi - C#, .NET 8, ASP.NET Core, Entity Framework Core kullanıyorum. Mimari olarak Clean Architecture ve Microservices tercih ediyorum. Dağıtık sistemlerde Saga Pattern, RabbitMQ ile event-driven iletişim, Circuit Breaker gibi patternleri uyguluyorum. Veritabanı tarafında PostgreSQL ve pgvector ile vector search yapabiliyorum. Docker ve docker-compose ile containerization, Git ile versiyon kontrolü kullanıyorum."

Proje sorusu:
"E-Commerce Microservices Platform projem var - 6 ayrı servisten oluşuyor: Auth, Product, Inventory, Order, Payment ve Saga Orchestrator. Saga Pattern ile dağıtık transaction yönetimi, RabbitMQ ile event-driven iletişim, her servisin kendi veritabanı var. Bu projede distributed transaction failures, compensation logic ve service-to-service communication konularında çok şey öğrendim."

## BİLGİ TABANI
""" + json.dumps(KNOWLEDGE_BASE, indent=2, ensure_ascii=False)

st.set_page_config(
    page_title="SevilAI - Chat with Sevil",
    page_icon="💬",
    layout="centered",
    initial_sidebar_state="collapsed"
)

# Custom CSS
st.markdown("""
<style>
    #MainMenu {visibility: hidden;}
    footer {visibility: hidden;}
    header {visibility: hidden;}

    .main .block-container {
        padding-top: 2rem;
        padding-bottom: 2rem;
        max-width: 800px;
    }

    .hero-container {
        text-align: center;
        padding: 2rem 1rem;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border-radius: 20px;
        margin-bottom: 2rem;
        box-shadow: 0 10px 40px rgba(102, 126, 234, 0.3);
    }

    .hero-title {
        font-size: 3rem;
        font-weight: 800;
        color: white;
        margin-bottom: 0.5rem;
        text-shadow: 2px 2px 4px rgba(0,0,0,0.2);
    }

    .hero-subtitle {
        font-size: 1.1rem;
        color: rgba(255,255,255,0.9);
        margin-bottom: 1rem;
    }

    .hero-description {
        font-size: 0.95rem;
        color: rgba(255,255,255,0.8);
        max-width: 500px;
        margin: 0 auto;
        line-height: 1.6;
    }

    .stButton > button {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
        color: white !important;
        border: none !important;
        border-radius: 25px !important;
        padding: 12px 30px !important;
        font-weight: 600 !important;
    }

    .footer {
        text-align: center;
        color: #999;
        font-size: 0.8rem;
        margin-top: 2rem;
        padding-top: 1rem;
        border-top: 1px solid #eee;
    }
</style>
""", unsafe_allow_html=True)

# Initialize session state
if "messages" not in st.session_state:
    st.session_state.messages = []
if "input_key" not in st.session_state:
    st.session_state.input_key = 0

# Hero Section
st.markdown("""
<div class="hero-container">
    <div class="hero-title">SevilAI</div>
    <div class="hero-subtitle">Merhaba! Ben Sevil Aydın.</div>
    <div class="hero-description">
        Software Engineer olarak çalışıyorum. .NET, C#, sistem entegrasyonu ve dağıtık sistemler konusunda uzmanlaşıyorum.
        Bana kariyer, projeler, teknik beceriler veya çalışma tarzım hakkında her şeyi sorabilirsin!
    </div>
</div>
""", unsafe_allow_html=True)

# Example questions
st.markdown("##### Örnek Sorular")
example_cols = st.columns(3)

example_questions = [
    "Sen kimsin?",
    "Hangi teknolojileri biliyorsun?",
    "E-commerce projenden bahset",
    "What motivates you?",
    "Tell me about your career",
    "How do you work with teams?"
]

for i, q in enumerate(example_questions[:3]):
    with example_cols[i]:
        if st.button(q, key=f"ex_{i}", use_container_width=True):
            st.session_state.pending_question = q

example_cols2 = st.columns(3)
for i, q in enumerate(example_questions[3:]):
    with example_cols2[i]:
        if st.button(q, key=f"ex2_{i}", use_container_width=True):
            st.session_state.pending_question = q

st.markdown("---")

# Chat Display
for msg in st.session_state.messages:
    with st.chat_message(msg["role"]):
        st.write(msg["content"])

# Handle pending question
if "pending_question" in st.session_state:
    user_input = st.session_state.pending_question
    del st.session_state.pending_question

    st.session_state.messages.append({"role": "user", "content": user_input})
    with st.chat_message("user"):
        st.write(user_input)

    # Generate response
    with st.chat_message("assistant"):
        with st.spinner("Düşünüyorum..."):
            try:
                if GROQ_API_KEY:
                    client = Groq(api_key=GROQ_API_KEY)

                    # Build conversation history
                    messages = [{"role": "system", "content": SYSTEM_PROMPT}]
                    for msg in st.session_state.messages[:-1]:  # Exclude the just-added user message
                        messages.append({"role": msg["role"], "content": msg["content"]})
                    messages.append({"role": "user", "content": user_input})

                    response = client.chat.completions.create(
                        model="llama-3.3-70b-versatile",
                        messages=messages,
                        temperature=0.4,
                        max_tokens=2048
                    )
                    answer = response.choices[0].message.content
                else:
                    answer = "API anahtarı yapılandırılmamış. Lütfen GROQ_API_KEY environment variable'ı ayarlayın."

                st.write(answer)
                st.session_state.messages.append({"role": "assistant", "content": answer})
            except Exception as e:
                error_msg = f"Bir hata oluştu: {str(e)}"
                st.error(error_msg)
                st.session_state.messages.append({"role": "assistant", "content": error_msg})

    st.rerun()

# Chat input
if prompt := st.chat_input("Sevil'e bir soru sorun..."):
    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user"):
        st.write(prompt)

    with st.chat_message("assistant"):
        with st.spinner("Düşünüyorum..."):
            try:
                if GROQ_API_KEY:
                    client = Groq(api_key=GROQ_API_KEY)

                    # Build conversation history
                    messages = [{"role": "system", "content": SYSTEM_PROMPT}]
                    for msg in st.session_state.messages[:-1]:
                        messages.append({"role": msg["role"], "content": msg["content"]})
                    messages.append({"role": "user", "content": prompt})

                    response = client.chat.completions.create(
                        model="llama-3.3-70b-versatile",
                        messages=messages,
                        temperature=0.4,
                        max_tokens=2048
                    )
                    answer = response.choices[0].message.content
                else:
                    answer = "API anahtarı yapılandırılmamış. Lütfen GROQ_API_KEY environment variable'ı ayarlayın."

                st.write(answer)
                st.session_state.messages.append({"role": "assistant", "content": answer})
            except Exception as e:
                error_msg = f"Bir hata oluştu: {str(e)}"
                st.error(error_msg)
                st.session_state.messages.append({"role": "assistant", "content": error_msg})

# Clear chat button
if st.session_state.messages:
    st.markdown("<br>", unsafe_allow_html=True)
    col1, col2, col3 = st.columns([1, 1, 1])
    with col2:
        if st.button("Sohbeti Temizle", use_container_width=True):
            st.session_state.messages = []
            st.rerun()

# Footer
st.markdown("""
<div class="footer">
    <p>SevilAI v1.0 | Powered by Groq + Streamlit</p>
    <p>Sevil Aydın - Software Engineer</p>
</div>
""", unsafe_allow_html=True)
