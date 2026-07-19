# DataIngestion — Project Context

## What this is
A data ingestion pipeline service that receives a webhook with a ZIP URL, downloads and parses JSON client files, and persists the data to SQLite via EF Core.

## Stack
- **Framework:** ASP.NET Core Web API (.NET 10) — controller-based
- **ORM:** Entity Framework Core with SQLite (`dataingestion.db` created on first run)
- **DI:** ASP.NET Core built-in (`Microsoft.Extensions.DependencyInjection`)
- **API Docs:** Swagger at `/swagger`

## Project structure
```
DataIngestion.Api/
├── Controllers/
│   └── WebhookController.cs     POST /api/webhook
├── DTOs/
│   └── PartnerDtos.cs           JSON shapes from partner + request/response types
├── Models/
│   ├── Client.cs                EF Core entity
│   ├── Account.cs               EF Core entity
│   └── Holding.cs               EF Core entity
├── Data/
│   └── AppDbContext.cs          EF Core DbContext, DB created via EnsureCreated()
├── Services/
│   ├── IIngestionService.cs     Interface for DI / testability
│   └── IngestionService.cs      Downloads ZIP, parses JSON, upserts to DB
├── wwwroot/
│   └── test-data.zip            5 sample clients for local testing
└── Program.cs                   DI registration, middleware config

DataIngestion.Tests/
├── Controllers/WebhookControllerTests.cs
├── Services/IngestionServiceTests.cs
├── Helpers/
│   ├── MockHttpMessageHandler.cs
│   └── TestDataBuilder.cs
└── DataIngestion.Tests.csproj
```

## How to run
```bash
cd DataIngestion.Api
dotnet run
# App starts on http://localhost:5141 (or check Properties/launchSettings.json)
# Swagger UI: http://localhost:5141/swagger
# DB file: DataIngestion.Api/dataingestion.db (created automatically)
```

## How to run tests
```bash
dotnet test
```

## How to test ingestion
Once running, POST to the webhook with the locally served test ZIP:
```bash
curl -X POST http://localhost:5141/api/webhook \
  -H "Content-Type: application/json" \
  -d '{"url": "http://localhost:5141/test-data.zip"}'
```
Expected response:
```json
{ "clientsProcessed": 5, "accountsProcessed": 10, "holdingsProcessed": 19 }
```

## Data flow
```
POST /api/webhook
  → WebhookController (validates, delegates)
  → IngestionService.IngestAsync(url)
      → HttpClient downloads ZIP bytes
      → ZipArchive extracts .json entries
      → JsonSerializer deserializes → ClientDto[]
      → For each client: upsert (update or insert) to DB
          → Existing accounts/holdings deleted, replaced
      → DbContext.SaveChangesAsync()
  → Returns IngestionResult (counts)
```

## Upsert strategy
- Match on `ClientId` (unique index)
- If client exists: update fields, delete old accounts/holdings, insert new ones
- If client is new: insert everything
- All changes saved in a single `SaveChangesAsync()` call per run

## Key decisions made
- **Awaited (not fire-and-forget):** ~1000 clients is fast enough; avoids background job complexity
- **EnsureCreated() not Migrate():** Simpler for demo; swap to Migrate() for production
- **SQLite:** Zero external dependencies, file-based. To switch to Postgres: change connection string + `UseNpgsql(...)`, regenerate migrations
- **DTOs separate from models:** Partner JSON shape doesn't dictate DB schema
- **HttpClient via DI:** Registered with `AddHttpClient<IIngestionService, IngestionService>()` for proper lifecycle management

## What's next (Phase 2)
- `GET /api/clients?page=1&pageSize=20` — paged client list
- `GET /api/clients/{clientId}/accounts` — accounts for a client
- `GET /api/clients/{clientId}/accounts/{accountId}/holdings` — holdings

## What's next (Phase 3 — UI)
- Decision pending: Razor Pages (same project) vs React (separate)
