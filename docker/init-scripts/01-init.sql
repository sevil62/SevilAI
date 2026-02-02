-- SevilAI Database Schema
-- PostgreSQL with pgvector extension for vector similarity search

-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Documents table: stores original source documents
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(500) NOT NULL,
    source_type VARCHAR(100) NOT NULL, -- 'cv', 'note', 'project', 'skill', 'experience'
    content TEXT NOT NULL,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Chunks table: stores document chunks for retrieval
CREATE TABLE chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    token_count INTEGER NOT NULL,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_document_chunk UNIQUE (document_id, chunk_index)
);

-- Embeddings table: stores vector embeddings for chunks
CREATE TABLE embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    chunk_id UUID NOT NULL REFERENCES chunks(id) ON DELETE CASCADE,
    embedding vector(384), -- 384 dimensions for MiniLM or similar
    model_name VARCHAR(200) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_chunk_embedding UNIQUE (chunk_id)
);

-- Skills table: structured skill data for quick lookups
CREATE TABLE skills (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    category VARCHAR(100) NOT NULL, -- 'language', 'framework', 'tool', 'domain', 'soft_skill'
    proficiency_level VARCHAR(50), -- 'expert', 'advanced', 'intermediate', 'beginner'
    years_experience DECIMAL(4,1),
    description TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_skill_name UNIQUE (name)
);

-- Experiences table: structured work experience data
CREATE TABLE experiences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company VARCHAR(300) NOT NULL,
    role VARCHAR(300) NOT NULL,
    period_start DATE,
    period_end DATE,
    is_current BOOLEAN DEFAULT FALSE,
    description TEXT,
    achievements TEXT[],
    technologies TEXT[],
    is_confidential BOOLEAN DEFAULT FALSE,
    nda_note TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Projects table: personal and professional projects
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(300) NOT NULL,
    project_type VARCHAR(100) NOT NULL, -- 'personal', 'enterprise', 'open_source'
    description TEXT,
    technologies TEXT[],
    features TEXT[],
    architecture_notes TEXT,
    is_confidential BOOLEAN DEFAULT FALSE,
    nda_note TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Query logs table: for analytics and improvement
CREATE TABLE query_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    query_text TEXT NOT NULL,
    response_text TEXT,
    chunks_used UUID[],
    confidence_score DECIMAL(5,4),
    generation_mode VARCHAR(50), -- 'llm', 'template', 'hybrid'
    latency_ms INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX idx_documents_source_type ON documents(source_type);
CREATE INDEX idx_chunks_document_id ON chunks(document_id);
CREATE INDEX idx_embeddings_chunk_id ON embeddings(chunk_id);
CREATE INDEX idx_skills_category ON skills(category);
CREATE INDEX idx_experiences_company ON experiences(company);
CREATE INDEX idx_projects_type ON projects(project_type);
CREATE INDEX idx_query_logs_created ON query_logs(created_at);

-- Create vector index for similarity search (using IVFFlat for good balance)
CREATE INDEX idx_embeddings_vector ON embeddings USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger for documents table
CREATE TRIGGER update_documents_updated_at
    BEFORE UPDATE ON documents
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Comments for documentation
COMMENT ON TABLE documents IS 'Original source documents (CV, notes, etc.)';
COMMENT ON TABLE chunks IS 'Document chunks for RAG retrieval';
COMMENT ON TABLE embeddings IS 'Vector embeddings for semantic search';
COMMENT ON TABLE skills IS 'Structured skill data for quick lookups and estimation';
COMMENT ON TABLE experiences IS 'Work experience records with NDA handling';
COMMENT ON TABLE projects IS 'Personal and enterprise projects';
COMMENT ON TABLE query_logs IS 'Query analytics and debugging';
