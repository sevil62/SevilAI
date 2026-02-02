import streamlit as st
import requests
import time

# Configuration
API_BASE_URL = "http://localhost:5159"

st.set_page_config(
    page_title="SevilAI - Chat with Sevil",
    page_icon="💬",
    layout="centered",
    initial_sidebar_state="collapsed"
)

# Custom CSS for beautiful chat interface
st.markdown("""
<style>
    /* Hide Streamlit branding */
    #MainMenu {visibility: hidden;}
    footer {visibility: hidden;}
    header {visibility: hidden;}

    /* Main container */
    .main .block-container {
        padding-top: 2rem;
        padding-bottom: 2rem;
        max-width: 800px;
    }

    /* Hero section */
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

    /* Chat container */
    .chat-container {
        background: #f8f9fa;
        border-radius: 16px;
        padding: 1.5rem;
        margin-bottom: 1rem;
        min-height: 400px;
        max-height: 500px;
        overflow-y: auto;
    }

    /* Message bubbles */
    .user-message {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 12px 18px;
        border-radius: 18px 18px 4px 18px;
        margin: 8px 0;
        margin-left: 20%;
        box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
    }

    .bot-message {
        background: white;
        color: #333;
        padding: 12px 18px;
        border-radius: 18px 18px 18px 4px;
        margin: 8px 0;
        margin-right: 20%;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
        border: 1px solid #e9ecef;
    }

    .bot-name {
        font-weight: 600;
        color: #667eea;
        font-size: 0.85rem;
        margin-bottom: 4px;
    }

    /* Input area */
    .stTextInput > div > div > input {
        border-radius: 25px !important;
        border: 2px solid #e9ecef !important;
        padding: 12px 20px !important;
        font-size: 1rem !important;
    }

    .stTextInput > div > div > input:focus {
        border-color: #667eea !important;
        box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.2) !important;
    }

    /* Button styling */
    .stButton > button {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
        color: white !important;
        border: none !important;
        border-radius: 25px !important;
        padding: 12px 30px !important;
        font-weight: 600 !important;
        font-size: 1rem !important;
        box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4) !important;
        transition: all 0.3s ease !important;
    }

    .stButton > button:hover {
        transform: translateY(-2px) !important;
        box-shadow: 0 6px 20px rgba(102, 126, 234, 0.5) !important;
    }

    /* Metrics */
    .metric-container {
        display: flex;
        justify-content: center;
        gap: 2rem;
        margin-top: 1rem;
        padding: 1rem;
        background: rgba(102, 126, 234, 0.05);
        border-radius: 12px;
    }

    .metric-item {
        text-align: center;
    }

    .metric-value {
        font-size: 1.2rem;
        font-weight: 700;
        color: #667eea;
    }

    .metric-label {
        font-size: 0.75rem;
        color: #666;
        text-transform: uppercase;
        letter-spacing: 0.5px;
    }

    /* Example questions */
    .example-btn {
        background: white !important;
        color: #667eea !important;
        border: 1px solid #667eea !important;
        border-radius: 20px !important;
        padding: 8px 16px !important;
        margin: 4px !important;
        font-size: 0.85rem !important;
        box-shadow: none !important;
    }

    .example-btn:hover {
        background: rgba(102, 126, 234, 0.1) !important;
    }

    /* Typing indicator */
    .typing-indicator {
        display: flex;
        gap: 4px;
        padding: 12px 18px;
        background: white;
        border-radius: 18px;
        width: fit-content;
        margin: 8px 0;
    }

    .typing-dot {
        width: 8px;
        height: 8px;
        background: #667eea;
        border-radius: 50%;
        animation: typing 1.4s infinite;
    }

    .typing-dot:nth-child(2) { animation-delay: 0.2s; }
    .typing-dot:nth-child(3) { animation-delay: 0.4s; }

    @keyframes typing {
        0%, 60%, 100% { transform: translateY(0); opacity: 0.4; }
        30% { transform: translateY(-4px); opacity: 1; }
    }

    /* Scrollbar */
    .chat-container::-webkit-scrollbar {
        width: 6px;
    }

    .chat-container::-webkit-scrollbar-track {
        background: #f1f1f1;
        border-radius: 3px;
    }

    .chat-container::-webkit-scrollbar-thumb {
        background: #c1c1c1;
        border-radius: 3px;
    }

    /* Footer */
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
    <div class="hero-title">💬 SevilAI</div>
    <div class="hero-subtitle">Merhaba! Ben Sevil Aydın.</div>
    <div class="hero-description">
        Software Engineer olarak çalışıyorum. .NET, C#, sistem entegrasyonu ve dağıtık sistemler konusunda uzmanlaşıyorum.
        Bana kariyer, projeler, teknik beceriler veya çalışma tarzım hakkında her şeyi sorabilirsin!
    </div>
</div>
""", unsafe_allow_html=True)

# Example questions
st.markdown("##### 💡 Örnek Sorular")
example_cols = st.columns(3)

example_questions_tr = [
    "Sen kimsin?",
    "Hangi teknolojileri biliyorsun?",
    "Projelerinden bahset"
]
example_questions_en = [
    "What motivates you?",
    "Tell me about your career",
    "How do you work with teams?"
]

for i, q in enumerate(example_questions_tr):
    with example_cols[i]:
        if st.button(q, key=f"ex_tr_{i}", use_container_width=True):
            st.session_state.pending_question = q

example_cols2 = st.columns(3)
for i, q in enumerate(example_questions_en):
    with example_cols2[i]:
        if st.button(q, key=f"ex_en_{i}", use_container_width=True):
            st.session_state.pending_question = q

st.markdown("---")

# Chat Display
chat_placeholder = st.container()

with chat_placeholder:
    for msg in st.session_state.messages:
        if msg["role"] == "user":
            st.markdown(f"""
            <div class="user-message">
                {msg["content"]}
            </div>
            """, unsafe_allow_html=True)
        else:
            st.markdown(f"""
            <div class="bot-message">
                <div class="bot-name">🤖 Sevil</div>
                {msg["content"]}
            </div>
            """, unsafe_allow_html=True)

            # Show metrics if available
            if "metrics" in msg:
                m = msg["metrics"]
                st.markdown(f"""
                <div class="metric-container">
                    <div class="metric-item">
                        <div class="metric-value">{m['confidence']:.0%}</div>
                        <div class="metric-label">Güven</div>
                    </div>
                    <div class="metric-item">
                        <div class="metric-value">{m['latency']}ms</div>
                        <div class="metric-label">Süre</div>
                    </div>
                    <div class="metric-item">
                        <div class="metric-value">{m['chunks']}</div>
                        <div class="metric-label">Kaynak</div>
                    </div>
                </div>
                """, unsafe_allow_html=True)

st.markdown("---")

# Input Section
col1, col2 = st.columns([5, 1])

with col1:
    user_input = st.text_input(
        "Mesajınız",
        placeholder="Sevil'e bir soru sorun...",
        key=f"user_input_{st.session_state.input_key}",
        label_visibility="collapsed"
    )

with col2:
    send_button = st.button("Sor", type="primary", use_container_width=True)

# Handle pending question from example buttons
if "pending_question" in st.session_state:
    user_input = st.session_state.pending_question
    del st.session_state.pending_question
    send_button = True

# Process message
if (send_button or user_input) and user_input and user_input.strip():
    # Add user message
    st.session_state.messages.append({
        "role": "user",
        "content": user_input
    })

    # Call API
    try:
        with st.spinner(""):
            response = requests.post(
                f"{API_BASE_URL}/api/ask",
                json={
                    "question": user_input,
                    "topK": 5,
                    "minSimilarity": 0.3,
                    "useLLM": True,
                    "includeSources": False
                },
                timeout=60
            )

            if response.status_code == 200:
                data = response.json()

                # Add bot response
                st.session_state.messages.append({
                    "role": "assistant",
                    "content": data["answer"],
                    "metrics": {
                        "confidence": data["confidenceScore"],
                        "latency": data["latencyMs"],
                        "chunks": data["metadata"]["chunksRetrieved"]
                    }
                })
            else:
                st.session_state.messages.append({
                    "role": "assistant",
                    "content": f"Üzgünüm, bir hata oluştu. (Hata kodu: {response.status_code})"
                })

    except requests.exceptions.ConnectionError:
        st.session_state.messages.append({
            "role": "assistant",
            "content": "API'ye bağlanılamıyor. Lütfen API'nin çalıştığından emin olun. (http://localhost:5159)"
        })
    except Exception as e:
        st.session_state.messages.append({
            "role": "assistant",
            "content": f"Bir hata oluştu: {str(e)}"
        })

    # Clear input and rerun
    st.session_state.input_key += 1
    st.rerun()

# Clear chat button
if st.session_state.messages:
    st.markdown("<br>", unsafe_allow_html=True)
    col1, col2, col3 = st.columns([1, 1, 1])
    with col2:
        if st.button("🗑️ Sohbeti Temizle", use_container_width=True):
            st.session_state.messages = []
            st.rerun()

# Footer
st.markdown("""
<div class="footer">
    <p>💜 SevilAI v1.0 | .NET 8 + Groq + Streamlit</p>
    <p>Sevil Aydın - Software Engineer</p>
</div>
""", unsafe_allow_html=True)
