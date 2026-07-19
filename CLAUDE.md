# DataIngestion — Project Context

## What this is
A data ingestion pipeline service that receives a webhook with a ZIP URL, downloads and parses JSON client files, and persists the data to SQLite via EF Core. Includes a Razor Pages UI for triggering ingestion and browsing client/account/holding data with historical as-of queries.

## Stack
- **Framework:** ASP.NET Core Web API + Razor Pages (.NET 10)
- **ORM:** Entity Framework Core with SQLite (`dataingestion.db` created on first run)
- **DI:** ASP.NET Core built-in (`Microsoft.Extensions.DependencyInjection`)
- **UI:** Razor Pages + Bootstrap 5 + DataTables (CDN)
- **Tests:** xUnit + Moq (37 tests)
- **API Docs:** Swagger at `/swagger`

## Project structure
```
DataIngestion.Api/
├── Controllers/
│   ├── WebhookController.cs      POST /api/webhook
│   └── ClientsController.cs      GET /api/clients, /accounts, /holdings
├── DTOs/
│   └── PartnerDtos.cs            JSON shapes from partner + request/response types
├── Models/
│   ├── IngestionRun.cs           EF Core entity — envelope for one delivery
│   ├── Client.cs                 EF Core entity (FK → IngestionRun)
│   ├── Account.cs                EF Core entity
│   └── Holding.cs                EF Core entity
├── Data/
│   └── AppDbContext.cs           EF Core DbContext, DB created via EnsureCreated()
├── Services/
│   ├── IIngestionService.cs      Interface — two overloads (URL and bytes)
│   ├── IngestionService.cs       Downloads ZIP, parses JSON, inserts fresh per run
│   ├── IClientQueryService.cs    Interface — all queries accept optional asOf
│   └── ClientQueryService.cs     Scopes all queries to latest (or as-of) run
├── Pages/
│   ├── Index.cshtml / .cs        Dashboard — ingestion form + clients grid
│   └── Clients/
│       ├── Accounts.cshtml / .cs Accounts for a client
│       └── Holdings.cshtml / .cs Holdings for an account
├── wwwroot/
│   ├── test-data-v1.zip          5 clients (original snapshot)
│   └── test-data-v2.zip          5 clients (Emily Wilson → Alex Thompson, Jane Smith updated)
└── Program.cs                    DI registration, middleware config

DataIngestion.Tests/
├── Controllers/
│   ├── WebhookControllerTests.cs
│   └── ClientsControllerTests.cs
├── Services/
│   ├── IngestionServiceTests.cs
│   └── ClientQueryServiceTests.cs
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
# UI:      http://localhost:5141
# Swagger: http://localhost:5141/swagger
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
  -d '{"url": "http://localhost:5141/test-data-v1.zip"}'
```

## Data flow
```
POST /api/webhook
  → WebhookController (validates, delegates)
  → IngestionService.IngestAsync(url)
      → HttpClient downloads ZIP bytes
      → ZipArchive extracts .json entries
      → JsonSerializer deserializes → ClientDto[]
      → Creates IngestionRun (KnowledgeDate = UtcNow)
      → For each client: insert fresh rows (append-only, no upsert)
      → DbContext.SaveChangesAsync()
  → Returns IngestionResult (counts, RunId, KnowledgeDate)
```

## Append-only ingestion strategy
- Each `POST /api/webhook` creates a new `IngestionRun` row
- All clients/accounts/holdings are inserted fresh — nothing is updated or deleted
- "Current" data = rows scoped to `MAX(IngestionRun.Id)`
- "Historical" data = rows scoped to the latest run on or before a given date
- Composite unique index on `(ClientId, IngestionRunId)` — same client can appear in multiple runs

## As-of / historical queries
`IClientQueryService` methods accept `DateTimeOffset? asOf = null`:
- `null` → latest run (`MAX(Id)`)
- date provided → latest run where `KnowledgeDate <= asOf` (resolved client-side because SQLite cannot translate `DateTimeOffset` comparisons to SQL)

The UI exposes this via a `?asOf=` query parameter that flows through Index → Accounts → Holdings pages, so the breadcrumb navigation preserves the historical context.

## Key decisions made
- **Awaited (not fire-and-forget):** ~1000 clients is fast enough; avoids background job complexity
- **Append-only not upsert:** Preserves full snapshot history; enables as-of queries with no extra work
- **EnsureCreated() not Migrate():** Simpler for demo; swap to Migrate() for production
- **SQLite:** Zero external dependencies, file-based. To switch to Postgres: change connection string + `UseNpgsql(...)`, regenerate migrations
- **DTOs separate from models:** Partner JSON shape doesn't dictate DB schema
- **HttpClient via DI:** Registered with `AddHttpClient<IIngestionService, IngestionService>()` for proper lifecycle management
- **File upload via `Request.Form.Files`:** `[BindProperty] IFormFile` was unreliable; raw `Request.Form.Files.GetFile("ZipFile")` is used instead
- **`ShowInputError` boolean:** Used instead of `!ModelState.IsValid` to avoid false positives on the ingestion form

## Test data files (wwwroot/)

Two ZIPs for demoing the append-only / as-of history feature:

- **test-data-v1.zip** — 5 clients: CLT-29481 (Jane Smith), CLT-30012, CLT-30155, CLT-31000, CLT-31500 (Emily Wilson)
- **test-data-v2.zip** — same 5 slots but CLT-31500 (Emily Wilson) replaced by CLT-32000 (Alex Thompson); Jane Smith updated

### What changed in Jane Smith (CLT-29481) between v1 and v2

| Field | V1 | V2 |
|---|---|---|
| `last_updated` | 2025-03-02 | 2025-06-15 |
| `cash_balance` (ACC-10042) | 2,450.75 | 1,850.50 |
| `total_value` (ACC-10042) | 61,100.75 | 62,885.50 |
| BND holding | 200 shares | removed |
| MSFT holding | — | added |
| VTI quantity | 150 | 175 |
| VTI market value | 38,250 | 45,675 |
| VXUS quantity | 100 | 120 |
| VXUS market value | 5,600 | 6,960 |

Account ACC-10043 (ROTH IRA / VOO) and clients CLT-30012, CLT-30155, CLT-31000 are identical between v1 and v2.

### Demo sequence for as-of feature
1. Ingest v1 → note Run #1 knowledge date
2. Ingest v2 → grid updates (Emily Wilson gone, Alex Thompson in, Jane Smith updated)
3. Set date picker to a time between the two runs → grid snaps back to Run #1 (Emily Wilson back, Jane Smith's old holdings)
4. Click a client row → Accounts page preserves the as-of context
5. Click **Latest** → returns to current view

## Known SQLite limitation
SQLite via EF Core cannot translate `DateTimeOffset` comparisons (`<=`, `>=`) to SQL. Workarounds used:
- `LatestRunIdAsync` uses `MaxAsync(r => (int?)r.Id)` (not `OrderByDescending(KnowledgeDate)`)
- `RunIdAsOfAsync(asOf)` loads `(Id, KnowledgeDate)` for all runs client-side, then filters in memory — acceptable since run count stays small
