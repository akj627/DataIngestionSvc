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

Four ZIPs covering a progression of client portfolio changes for demoing append-only / as-of history.

### Client roster across versions

| Client | v1 | v2 | v3 | v4 |
|---|---|---|---|---|
| CLT-29481 Jane Smith | ✓ | ✓ updated | ✓ same as v2 | ✓ updated |
| CLT-30012 Michael Chen | ✓ | ✓ same | removed | — |
| CLT-30155 Sarah Johnson | ✓ | ✓ same | ✓ same | ✓ same |
| CLT-31000 David Martinez | ✓ | ✓ same | ✓ same | removed |
| CLT-31500 Emily Wilson | ✓ | removed | — | — |
| CLT-32000 Alex Thompson | — | ✓ new | ✓ updated | ✓ same as v3 |
| CLT-33000 Maria Garcia | — | — | ✓ new | ✓ updated |
| CLT-34000 Robert Chen | — | — | — | ✓ new |

### Changes per version

**v1 → v2** (2025-03-02 → 2025-06-15)
- CLT-31500 Emily Wilson **removed**; CLT-32000 Alex Thompson **added** (2 accounts: INDIVIDUAL + TRADITIONAL_IRA)
- CLT-29481 Jane Smith ACC-10042: BND removed, MSFT added; VTI 150→175; VXUS 100→120; last_updated changed

**v2 → v3** (2025-06-15 → 2025-09-10)
- CLT-30012 Michael Chen **removed**; CLT-33000 Maria Garcia **added** (2 accounts: INDIVIDUAL + ROTH_IRA; holdings: VOO, VTI, BND)
- CLT-32000 Alex Thompson ACC-60001: NVDA added (50 shares @ $875); QQQ 45→50 shares; cash_balance 3,200→1,500; last_updated changed

**v3 → v4** (2025-09-10 → 2025-11-20)
- CLT-31000 David Martinez **removed**; CLT-34000 Robert Chen **added** (2 accounts: INDIVIDUAL + TRADITIONAL_IRA; tech-heavy: AAPL, MSFT, NVDA, VOO, BND)
- CLT-29481 Jane Smith ACC-10042: MSFT removed, AMZN added (60 shares); VTI 175→200; VXUS 120→100; last_updated changed
- CLT-33000 Maria Garcia ACC-70001: SPY added (30 shares @ $460); VTI 100→120; last_updated changed

### Demo sequence for as-of feature
1. Ingest v1 → 5 clients, Emily Wilson visible
2. Ingest v2 → Emily Wilson gone, Alex Thompson in, Jane Smith updated
3. Ingest v3 → Michael Chen gone, Maria Garcia in, Alex Thompson has NVDA
4. Ingest v4 → David Martinez gone, Robert Chen in, Jane Smith rebalanced, Maria Garcia adds SPY
5. Set date picker between any two runs → grid snaps to that snapshot
6. Click a client → Accounts/Holdings pages preserve the as-of context
7. Try ingesting v2 again → duplicate warning (same ZIP hash rejected)

## Known SQLite limitation
SQLite via EF Core cannot translate `DateTimeOffset` comparisons (`<=`, `>=`) to SQL. Workarounds used:
- `LatestRunIdAsync` uses `MaxAsync(r => (int?)r.Id)` (not `OrderByDescending(KnowledgeDate)`)
- `RunIdAsOfAsync(asOf)` loads `(Id, KnowledgeDate)` for all runs client-side, then filters in memory — acceptable since run count stays small
