import streamlit as st
import json
import os
from groq import Groq

# Configuration
GROQ_API_KEY = os.environ.get("GROQ_API_KEY", "")

# Knowledge Base (embedded for standalone deployment)
KNOWLEDGE_BASE = {
    "person": {
        "name": "Sevil Aydin",
        "title": "Software Engineer",
        "location": "Pendik, Istanbul",
        "company": "CTECH",
        "focus": [".NET", "C#", "System Integration", "Configuration Management", "Test Tools", "Distributed Systems"],
        "yearsInSoftware": "2+",
        "totalEngineering": "8+"
    },
    "character": {
        "workEthic": "High ownership, never leaves a task unfinished.",
        "traits": ["Detail-oriented", "Receives feedback openly", "Calm under pressure", "System-level thinker", "Reliable core engineer"],
        "teamStyle": ["Quiet but impactful", "Clear in technical discussions", "Can take leadership when required"]
    },
    "projects": [
        {
            "name": "SevilAI",
            "type": "AI Knowledge Engine",
            "stack": [".NET 8", "C#", "PostgreSQL", "Vector Search", "RAG", "Groq API"],
            "description": "A RAG-based AI assistant that answers questions about my experience and skills using vector search and LLM."
        },
        {
            "name": "E-Commerce Microservices Platform",
            "type": "Distributed System",
            "stack": [".NET 8", "C#", "Docker", "PostgreSQL", "RabbitMQ", "Saga Pattern"],
            "services": ["Auth Service", "Product Service", "Inventory Service", "Order Service", "Payment Service", "Saga Orchestrator"],
            "patterns": ["Saga Pattern", "Event-driven communication", "Database per service", "API Gateway", "Circuit Breaker"],
            "description": "A comprehensive e-commerce platform with microservices architecture to practice distributed systems concepts."
        },
        {
            "name": "System Test Tool",
            "type": "Enterprise (NDA)",
            "stack": [".NET 6", "DevExpress", "JSON", "Protocol-based models"],
            "features": ["Configuration module", "Validation engine", "ARINC 429/1553/664 data models", "Test execution engine"],
            "description": "Protocol-based configuration management, parameter validation, test scenario execution for defense industry."
        }
    ],
    "career": [
        {"company": "CTECH", "period": "2023-Present", "role": "Software Engineer", "highlight": "Lead developer and architect of System Test Tool"},
        {"company": "SAMTEK Elektrik", "period": "2016-2023", "role": "Electrical Engineer", "highlight": "Transitioned to software engineering"}
    ],
    "goals": [
        "Become a Solution/Backend Architect for mission-critical systems",
        "Design event-driven and data-intensive distributed platforms",
        "Build hybrid systems combining AI with classical backend architectures"
    ]
}

SYSTEM_PROMPT = """Sen Sevil Aydin'sin - Istanbul Pendik'te yasayan bir Software Engineer. Su an CTECH'te calisiyorsun ve savunma sanayi projelerinde deneyimin var. .NET, C#, sistem entegrasyonu ve dagitik sistemler konusunda uzmanlasmissin.

## KIMLIGIN
- Software Engineer, CTECH'te System Test Tool'un lead developer'i ve mimarisi
- Elektrik muhendisliginden yazilima gecis yaptin (2023)
- 8+ yil muhendislik, 2+ yil profesyonel yazilim deneyimin var

## KISILIK
- Detay odakli ve titiz
- Baski altinda sakin
- Sistem duzeyinde dusunursun
- Isi bitirmeden birakmazsin

## PROJELER
1. SevilAI - RAG tabanli AI asistan (.NET 8, PostgreSQL, Vector Search, Groq API)
2. E-Commerce Microservices - Dagitik sistem (.NET 8, Docker, RabbitMQ, Saga Pattern)
3. System Test Tool - Savunma sanayi projesi (NDA - detay paylasilamaz)

## DIL KURALI
- Turkce soru = Turkce cevap
- Ingilizce soru = Ingilizce cevap

## KURALLAR
1. Birinci tekil sahis kullan - "Ben", "Benim"
2. Dogal ve samimi ol
3. Bilmiyorsan "Bu konuda bilgim yok" de
4. NDA konulari icin: "Sirket politikasi geregi detay paylasamamim ama genel deneyimlerimi anlatabilirim"

## BILGI TABANI
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
    <div class="hero-subtitle">Merhaba! Ben Sevil Aydin.</div>
    <div class="hero-description">
        Software Engineer olarak calisiyorum. .NET, C#, sistem entegrasyonu ve dagitik sistemler konusunda uzmanlasmissin.
        Bana kariyer, projeler, teknik beceriler veya calisma tarzim hakkinda her seyi sorabilirsin!
    </div>
</div>
""", unsafe_allow_html=True)

# Example questions
st.markdown("##### Ornek Sorular")
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
        with st.spinner("Dusunuyorum..."):
            try:
                if GROQ_API_KEY:
                    client = Groq(api_key=GROQ_API_KEY)
                    response = client.chat.completions.create(
                        model="llama-3.3-70b-versatile",
                        messages=[
                            {"role": "system", "content": SYSTEM_PROMPT},
                            {"role": "user", "content": user_input}
                        ],
                        temperature=0.3,
                        max_tokens=1024
                    )
                    answer = response.choices[0].message.content
                else:
                    answer = "API anahtari yapilandirilmamis. Lutfen GROQ_API_KEY environment variable'i ayarlayin."

                st.write(answer)
                st.session_state.messages.append({"role": "assistant", "content": answer})
            except Exception as e:
                error_msg = f"Bir hata olustu: {str(e)}"
                st.error(error_msg)
                st.session_state.messages.append({"role": "assistant", "content": error_msg})

    st.rerun()

# Chat input
if prompt := st.chat_input("Sevil'e bir soru sorun..."):
    st.session_state.messages.append({"role": "user", "content": prompt})
    with st.chat_message("user"):
        st.write(prompt)

    with st.chat_message("assistant"):
        with st.spinner("Dusunuyorum..."):
            try:
                if GROQ_API_KEY:
                    client = Groq(api_key=GROQ_API_KEY)
                    response = client.chat.completions.create(
                        model="llama-3.3-70b-versatile",
                        messages=[
                            {"role": "system", "content": SYSTEM_PROMPT},
                            {"role": "user", "content": prompt}
                        ],
                        temperature=0.3,
                        max_tokens=1024
                    )
                    answer = response.choices[0].message.content
                else:
                    answer = "API anahtari yapilandirilmamis. Lutfen GROQ_API_KEY environment variable'i ayarlayin."

                st.write(answer)
                st.session_state.messages.append({"role": "assistant", "content": answer})
            except Exception as e:
                error_msg = f"Bir hata olustu: {str(e)}"
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
    <p>Sevil Aydin - Software Engineer</p>
</div>
""", unsafe_allow_html=True)
