# ENV_SETUP.md
# Codify — Local Development Setup

> Complete this guide before writing any code. If something is wrong or missing, update this file.

---

## Prerequisites

Install these tools before anything else:

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org |
| Angular CLI | 17+ | `npm install -g @angular/cli` |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| PostgreSQL | 15+ | Via Docker (see below) or direct install |
| Git | Latest | https://git-scm.com |

---

## Step 1: Clone the Repo

```bash
git clone https://github.com/your-org/codify.git
cd codify
git checkout develop
```

---

## Step 2: Start Infrastructure (Docker)

Start PostgreSQL and Chroma with Docker Compose:

```bash
docker compose up -d
```

This starts:
- PostgreSQL on port `5432`
- Chroma vector DB on port `8000`

To stop:
```bash
docker compose down
```

---

## Step 3: Backend Setup

### Configure Environment Variables

Copy the example config:
```bash
cd backend
cp appsettings.Example.json src/Codify.API/appsettings.Development.json
```

Fill in your values in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=codify_dev;Username=codify;Password=codify123"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars-long",
    "Issuer": "codify-api",
    "ExpiryHours": 24
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o"
  },
  "VectorDB": {
    "ChromaUrl": "http://localhost:8000"
  },
  "Docker": {
    "SocketPath": "/var/run/docker.sock"
  }
}
```

> ⚠️ Never commit this file. It's in `.gitignore`.

### Run Database Migrations

```bash
cd src/Codify.API
dotnet ef database update
```

### Seed Initial Data

```bash
dotnet run --seed
```

This seeds: 12 concept tags, 10 sample problems with test cases, 2 demo users.

**Demo accounts:**
- Student: `student@codify.dev` / `Password123!`
- Instructor: `instructor@codify.dev` / `Password123!`

### Start the Backend

```bash
dotnet run
```

API runs at: `https://localhost:5001`  
Swagger UI: `https://localhost:5001/swagger`

---

## Step 4: Frontend Setup

```bash
cd frontend
npm install
```

Create your environment file:
```bash
cp src/environments/environment.example.ts src/environments/environment.development.ts
```

Contents:
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};
```

Start the frontend:
```bash
ng serve
```

App runs at: `http://localhost:4200`

---

## Step 5: Verify Everything Works

1. Open `http://localhost:4200`
2. Log in with `student@codify.dev` / `Password123!`
3. You should see the problem list
4. Open `https://localhost:5001/swagger` to test backend directly

---

## Required Environment Variables (Reference)

| Variable | Description | Who needs it |
|----------|-------------|--------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | Backend |
| `Jwt:Secret` | JWT signing key (min 32 chars) | Backend |
| `OpenAI:ApiKey` | OpenAI API key | Backend (Omar's setup) |
| `OpenAI:Model` | LLM model name | Backend |
| `VectorDB:ChromaUrl` | Chroma host URL | Backend |
| `Docker:SocketPath` | Docker socket (execution engine) | Backend (Badry's setup) |

---

## Common Issues

**`dotnet ef` not found:**
```bash
dotnet tool install --global dotnet-ef
```

**PostgreSQL connection refused:**
Make sure Docker is running: `docker compose up -d`

**Angular CLI not found:**
```bash
npm install -g @angular/cli
```

**SSL certificate error on localhost:**
```bash
dotnet dev-certs https --trust
```

**Chroma not responding:**
Check it's running: `docker compose ps` — look for the `chroma` container.

---

## docker-compose.yml (Reference)

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: codify_dev
      POSTGRES_USER: codify
      POSTGRES_PASSWORD: codify123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  chroma:
    image: chromadb/chroma:latest
    ports:
      - "8000:8000"
    volumes:
      - chroma_data:/chroma/chroma

volumes:
  postgres_data:
  chroma_data:
```
