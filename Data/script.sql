--docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres pgvector/pgvector:pg17   
--CREATE EXTENSION IF NOT EXISTS vector;

-- CREATE TABLE recomendations (
--     id SERIAL PRIMARY KEY,
--     title TEXT NOT NULL,
--     category TEXT NOT NULL,
--     embedding vector(1024) NOT NULL
-- );