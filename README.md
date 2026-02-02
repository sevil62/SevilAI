# SevilAI - RAG-Based AI Knowledge Engine

[![Streamlit App](https://static.streamlit.io/badges/streamlit_badge_black_white.svg)](https://sevilai.streamlit.app/)

A portfolio-grade, production-ready knowledge engine that answers questions about Sevil Aydin's professional profile using Retrieval-Augmented Generation (RAG).

## Live Demo

**Try it now:** [https://sevilai.streamlit.app/](https://sevilai.streamlit.app/)

Ask me anything about my career, projects, skills, and experience!

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
├── streamlit/
│   └── app.py                 # Streamlit Chat UI (deployed)
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

## Technology Stack

| Layer | Technology |
|-------|------------|
| **Backend** | .NET 8, ASP.NET Core Web API |
| **Database** | PostgreSQL 16 with pgvector extension |
| **Vector Search** | Cosine similarity on 384-dimensional embeddings |
| **LLM** | Groq API (llama-3.3-70b-versatile) |
| **Frontend** | Streamlit (Python) |
| **Deployment** | Streamlit Community Cloud |

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

### 2. Projects

| Project | Type | Stack |
|---------|------|-------|
| **SevilAI** | AI Knowledge Engine | .NET 8, PostgreSQL, Vector Search, RAG, Groq API |
| **E-Commerce Microservices** | Distributed System | .NET 8, Docker, RabbitMQ, Saga Pattern |
| **System Test Tool** | Enterprise (NDA) | .NET 6, DevExpress, Protocol-based models |

### 3. Answer Generation Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| **LLM** | Natural language generation using retrieved context | Production with API key |
| **NoLLM** | Template-based response builder | Free/offline usage |

## Getting Started

### Quick Start (Local)

1. **Clone the repository**
   ```bash
   git clone https://github.com/sevil62/SevilAI.git
   cd SevilAI
   ```

2. **Run the API**
   ```bash
   cd src/SevilAI.Api
   dotnet run --urls "http://localhost:5159"
   ```

3. **Run Streamlit UI**
   ```bash
   cd streamlit
   pip install -r requirements.txt
   export GROQ_API_KEY="your-groq-api-key"
   streamlit run app.py
   ```

4. **Open in browser**
   Navigate to http://localhost:8501

### Docker Deployment

```bash
cd docker
docker-compose up -d
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `GROQ_API_KEY` | Groq API key for LLM | Required |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | localhost |

### LLM Provider Setup (Groq - Free)

Get your free API key at [console.groq.com](https://console.groq.com)

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

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/ask` | POST | Ask a question about the knowledge base |
| `/api/estimate` | POST | Estimate project effort |
| `/api/seed/default` | POST | Seed from embedded resource |
| `/api/health` | GET | System health and stats |

## About Me

**Sevil Aydin** - Software Engineer

- Currently at CTECH (Defense Industry)
- Focus: .NET, C#, System Integration, Distributed Systems
- Lead developer and architect of System Test Tool
- 8+ years engineering experience, 2+ years in software

### Career Goals
- Become a Solution/Backend Architect for mission-critical systems
- Design event-driven and data-intensive distributed platforms
- Build hybrid systems combining AI with classical backend architectures

## Testing

```bash
cd tests/SevilAI.Tests
dotnet test
```

## License

MIT License - See LICENSE file for details.

---

Built with .NET 8 | PostgreSQL | pgvector | Groq | Streamlit

[Live Demo](https://sevilai.streamlit.app/) | [GitHub](https://github.com/sevil62/SevilAI)
