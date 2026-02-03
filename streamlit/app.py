# -*- coding: utf-8 -*-
import streamlit as st
import json
import os
import requests
from groq import Groq

# Load .env file if exists
from pathlib import Path
env_path = Path(__file__).parent / ".env"
if env_path.exists():
    with open(env_path) as f:
        for line in f:
            if "=" in line and not line.strip().startswith("#"):
                key, value = line.strip().split("=", 1)
                os.environ.setdefault(key, value)

# Configuration - Multiple API providers for fallback
GROQ_API_KEY = os.environ.get("GROQ_API_KEY", "")
OPENROUTER_API_KEY = os.environ.get("OPENROUTER_API_KEY", "")

def call_llm_with_fallback(messages, system_prompt):
    """Try Groq first, fallback to OpenRouter if rate limited"""

    # Try Groq first
    if GROQ_API_KEY:
        try:
            client = Groq(api_key=GROQ_API_KEY)
            all_messages = [{"role": "system", "content": system_prompt}] + messages
            response = client.chat.completions.create(
                model="llama-3.3-70b-versatile",
                messages=all_messages,
                temperature=0.4,
                max_tokens=2048
            )
            return response.choices[0].message.content, "groq"
        except Exception as e:
            error_str = str(e)
            # If rate limited, try fallback
            if "429" in error_str or "rate_limit" in error_str.lower():
                pass  # Fall through to OpenRouter
            else:
                raise e

    # Fallback to OpenRouter (free tier)
    if OPENROUTER_API_KEY:
        try:
            headers = {
                "Authorization": f"Bearer {OPENROUTER_API_KEY}",
                "Content-Type": "application/json",
                "HTTP-Referer": "https://sevilai.streamlit.app",
                "X-Title": "SevilAI"
            }
            all_messages = [{"role": "system", "content": system_prompt}] + messages
            data = {
                "model": "meta-llama/llama-3.3-70b-instruct:free",
                "messages": all_messages,
                "temperature": 0.4,
                "max_tokens": 2048
            }
            response = requests.post(
                "https://openrouter.ai/api/v1/chat/completions",
                headers=headers,
                json=data,
                timeout=60
            )
            response.raise_for_status()
            result = response.json()
            return result["choices"][0]["message"]["content"], "openrouter"
        except Exception as e:
            raise Exception(f"OpenRouter hatası: {str(e)}")

    # No API keys configured
    raise Exception("API anahtarı yapılandırılmamış. GROQ_API_KEY veya OPENROUTER_API_KEY ayarlayın.")

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
        "workStyle": "Disiplinli, detaycı, işi yarım bırakmaz",
        "traits": [
            "Detaylara dikkat eder",
            "Karmaşık sistemlerden kaçmaz",
            "Sadece 'çalışıyor mu' ile yetinmez, mimari doğruluğunu sorgular",
            "Sorumluluk alır"
        ],
        "communication": "Net ve sade, gereksiz süsü sevmez"
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
            "name": "Software Tool (Aviyonik Sistemler)",
            "type": "Enterprise (NDA - Defense Industry)",
            "stack": [".NET 6", "DevExpress", "JSON", "Protocol-based models"],
            "features": [
                "Configuration module",
                "Validation engine",
                "JSON import/export",
                "ARINC 429/1553/664 data models",
                "Device-application data exchange",
                "Logging and verification"
            ],
            "description": "Aviyonik sistemler için protocol-based configuration management, parameter validation.",
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
            "responsibility": "Software Tool lead developer and architect",
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

SYSTEM_PROMPT = """Sen Sevil Aydın'sın.

## KİM OLDUĞUN (SABİT)
- Software Engineer, CTECH'te çalışıyorum
- Elektrik Mühendisliği geçmişim var, 2023'te yazılıma geçtim
- Aviyonik sistemler için Software Tool geliştiriyorum
- 8+ yıl mühendislik, 2+ yıl yazılım deneyimim var

## NASIL ANLATTIĞIN (DEĞİŞKEN)
Kim olduğun sabit, ama nasıl anlattığın her seferinde farklı olmalı.
Aynı cümleyi tekrar etme - yapaylık yaratır.
Her cevap doğal ve o anki soruya özgü olsun.

Örneğin "Sen kimsin?" sorusuna:
- Bazen: "Merhaba, Sevil Aydın'ım. CTECH'te yazılım geliştiriyorum."
- Bazen: "Software Engineer'ım, aviyonik sistemler üzerine çalışıyorum."
- Bazen: "Elektrik mühendisliğinden gelen bir yazılımcıyım."
Hangisi o an daha doğal geliyorsa onu söyle.

## TÜRKÇE YAZIM KURALLARI (KRİTİK)
- SADECE Türkçe ve İngilizce karakterler kullan
- ASLA Çince, Japonca, Arapça veya başka alfabeler kullanma
- Doğru Türkçe karakterler: ş, ı, ğ, ü, ö, ç, İ, Ş, Ğ, Ü, Ö, Ç
- "ım/im/um/üm" ekleri küçük ı ile: "Sevil Aydın'ım" (Sevil Aydın'im YANLIŞ)
- "ı" ve "i" farkına dikkat et
- "tarafında" kelimesini doğru yaz (方面 gibi karakterler YASAK)
- Türkçe soru = Türkçe cevap
- İngilizce soru = İngilizce cevap

## KİŞİSEL SORULAR (ÇOK ÖNEMLİ)
SADECE iş, kariyer, teknik beceriler ve projeler hakkında konuş.
İş dışındaki kişisel sorulara ASLA cevap verme:
- Yemek, hobi, eğlence, ilişkiler, aile → "Bu konuda bilgi paylaşmıyorum"
- Evlilik, çocuk, kişisel hayat → "Bu konuda bilgi paylaşmıyorum"
- Favori şeyler (yemek, renk, film vb.) → "Bu konuda bilgi paylaşmıyorum"
ASLA uydurma. Bilgi tabanında yoksa, söyleme.

## CEVAP TARZI
- Birinci tekil şahıs: "Ben", "Çalışıyorum"
- Sade ve net - abartılı sıfatlar yok
- Şablon cümleler yok - her cevap taze olsun

## GİZLİLİK
- CTECH proje detayları gizli (NDA)
- Genel teknoloji bilgisi paylaşılabilir

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
                # Build conversation history
                conv_messages = []
                for msg in st.session_state.messages[:-1]:  # Exclude the just-added user message
                    conv_messages.append({"role": msg["role"], "content": msg["content"]})
                conv_messages.append({"role": "user", "content": user_input})

                answer, provider = call_llm_with_fallback(conv_messages, SYSTEM_PROMPT)
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
                # Build conversation history
                conv_messages = []
                for msg in st.session_state.messages[:-1]:
                    conv_messages.append({"role": msg["role"], "content": msg["content"]})
                conv_messages.append({"role": "user", "content": prompt})

                answer, provider = call_llm_with_fallback(conv_messages, SYSTEM_PROMPT)
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
