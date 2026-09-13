# SmeCore — reusable ASP.NET Core foundation for SME applications

Production-quality core (V1) intended as the starting point for multiple SME client
applications. It ships with authentication, users and permissions, Clients, Vehicles,
search/filter/sort/pagination, exports and a full audit trail, behind a responsive business UI.

**All user-facing text is European Portuguese (pt-PT).** Code identifiers, entities and
database columns follow the same language so the domain reads the same everywhere; comments and
this document are the exception, so a developer who doesn't speak Portuguese can still work on it.

---

## Stack

| Concern | Choice | Why |
|---|---|---|
| Runtime | .NET 8 (LTS) | Supported until Nov 2026, the safe default for App Platform |
| Web | ASP.NET Core Razor Pages | Server-rendered, no SPA build step, easy to hand over |
| Data | EF Core 8 + Npgsql + PostgreSQL 16 | Migrations committed, snake_case schema |
| Auth | ASP.NET Core Identity (Guid keys) | Cookie auth, lockout, password policy |
| Interactivity | HTMX 1.9 (vendored, 48 KB) | Only for list refresh and the client picker |
| Excel export | ClosedXML | Real `.xlsx`, not CSV renamed |
| Tests | xUnit + FluentAssertions | 105 tests; the data-layer ones run against real PostgreSQL |
| Styling | Hand-written CSS (`wwwroot/css/app.css`) | No Bootstrap, no CDN — a strict CSP would pass |

No microservices, no CQRS, no MediatR, no repository-over-a-repository. Three projects, one
service class per module.

---

## Solution layout

```
src/SmeCore.Domain           Entities, enums, validation (NIF, matrícula, código postal),
                             paging contracts, pt-PT formatting, the clock abstraction.
                             No dependencies on anything.

src/SmeCore.Infrastructure   EF Core (DbContext, configurations, migrations, audit interceptor),
                             Identity + permission system, module services
                             (ClientesServico, VeiculosServico, HistoricoServico,
                             UtilizadoresServico), CSV/XLSX exporters.

src/SmeCore.Web              Razor Pages, layout, CSS/JS, page models. Thin: pages call a
                             service and render.

tests/SmeCore.Tests          Unit tests (always run) + database tests (run when
                             TEST_POSTGRES_URL is set, otherwise reported as skipped).
```

---

## Running it locally

Requirements: .NET 8 SDK and a PostgreSQL 16 instance.

```bash
# 1. A database (any PostgreSQL 16 will do)
docker run -d --name smecore-pg -p 5432:5432 \
  -e POSTGRES_USER=smecore -e POSTGRES_PASSWORD=smecore -e POSTGRES_DB=smecore \
  postgres:16-alpine

# 2. Configuration — everything comes from environment variables, nothing is committed
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=smecore;Username=smecore;Password=smecore"
export Administrador__Email="admin@exemplo.pt"
export Administrador__PalavraPasse="ESCOLHA-UMA-FORTE"
export Seed__DadosDemonstracao=true      # optional: 28 clients + ~50 vehicles of sample data

# 3. Run — migrations are applied automatically on startup
dotnet run --project src/SmeCore.Web
```

Then open <http://localhost:5000> and sign in with the account above.

`Seed:DadosDemonstracao` only seeds when the `clientes` table is empty, and the sample
inspection dates are generated **relative to today**, so the dashboard and the "overdue
inspection" filters always have something to show, whatever day you run it.

### Tests

```bash
dotnet test                                    # unit tests; database tests reported as skipped

TEST_POSTGRES_URL="Host=localhost;Port=5432;Database=smecore_testes;Username=smecore;Password=smecore" \
  dotnet test                                  # the whole suite, against real PostgreSQL
```

The data-layer tests deliberately refuse to run on an in-memory provider: `ILIKE`, `jsonb`,
filtered unique indexes and identity sequences do not exist there, and testing them against a
substitute would be false confidence. Without the variable they are **skipped**, never passed.

### Migrations

```bash
dotnet ef migrations add NomeDaMigracao \
  --project src/SmeCore.Infrastructure --startup-project src/SmeCore.Web \
  --output-dir Dados/Migracoes
```

A design-time factory (`FabricaDesignTime`) is included so the tooling does not have to boot
the whole application — without it, `dotnet ef` would run the startup migration and seed.

---

## Configuration reference

Every setting can be supplied as an environment variable using the `__` separator.

| Key | Required | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | yes¹ | Npgsql connection string |
| `DATABASE_URL` | yes¹ | `postgres://…` URL — what DigitalOcean injects; converted automatically |
| `Administrador__Email` | first run | Initial administrator account |
| `Administrador__PalavraPasse` | first run | Its password. **Never** commit it |
| `Administrador__Nome` | no | Display name (default: "Administrador") |
| `BaseDados__MigrarNoArranque` | no | `true` (default) applies pending migrations at startup |
| `Seed__DadosDemonstracao` | no | `false` (default). `true` seeds sample data into an empty database |

¹ one of the two. If neither is present the application fails fast at startup rather than
serving pages against no database.

If no administrator is configured **and** no user exists, the application logs a warning and
creates nothing: an account with a guessable password on an internet-facing system is worse
than no account at all.

---

## Deploying to DigitalOcean App Platform

`.do/app.yaml` is a working spec: a Docker service plus a managed PostgreSQL 16 database,
with `${db.DATABASE_URL}` wired into the app and `/health` as the health check.

```bash
doctl apps create --spec .do/app.yaml
```

Set `Administrador__PalavraPasse` as a **SECRET** in the App Platform panel before the first
deploy, and point `github.repo` at the organisation's repository.

Notes that matter in that environment:

- The app runs behind DigitalOcean's load balancer, so `UseForwardedHeaders` is enabled —
  redirects come out as `https` and the audit trail records the real client IP, not the proxy's.
- The container installs `tzdata` and sets `TZ=Europe/Lisbon`. Without it the base image has no
  timezone database, `Europe/Lisbon` cannot be resolved and every date would be shown in UTC —
  an hour off during summer time.
- `/health` returns JSON that names the application and reports the database check, so it is
  obvious *which* service answered.

---

## How the foundation is meant to be reused

### Adding a module (Quotes, Work Orders, Stock…)

1. **Entity** in `SmeCore.Domain`, inheriting `EntidadeAuditavel`. That alone gives you
   created/changed by whom and when, soft deletion, and automatic history — the audit
   interceptor picks it up with no extra code.
2. **EF configuration** in `SmeCore.Infrastructure/Dados/Configuracoes`. It is discovered
   automatically (`ApplyConfigurationsFromAssembly`); snake_case naming is applied globally.
3. **Permissions**: add a `Modulo` entry to `Permissoes.Modulos`. The profile screen, the
   `[Authorize(Policy = …)]` policies and the "my permissions" list all pick it up with no
   further changes.
4. **Service** modelled on `ClientesServico`: a `MapaOrdenacao<T>` (the allow-list of sortable
   columns, with a mandatory tie-breaker), a filter class deriving from `ParametrosListagem`,
   a projection for the list, and `ParaPaginaAsync`.
5. **Pages**: copy `Pages/Clientes` — `Index` + `_Tabela` (list, filters, HTMX refresh,
   CSV/XLSX export), `Editar` (create and edit on one page, two routes), `Detalhes` (tabs
   including history). The pager, the sortable headers and the export plumbing are shared.
6. **Register** the service in `DependenciasInfrastructure` — one line.

### Pieces you get for free

- **`IRelogio`** — the single source of "now". Nothing calls `DateTime.Now`. It also converts
  a local calendar day into the correct UTC instants, so a "13/09 to 13/09" filter really means
  the Portuguese day and not 23:00-to-23:00.
- **`Formatos`** — every date, number and enum shown on screen goes through it. It is what
  keeps ISO dates (`2026-09-13`) off the UI; there is a test asserting the rendered pages
  contain none.
- **Audit trail** — a `SaveChanges` interceptor. Pages never remember to log anything, which is
  why the history can be trusted. Password hashes and security stamps are excluded by name;
  database-generated columns are skipped on insert (otherwise every creation would record
  "Número: 0"); the JSON is written unescaped so accented values remain searchable.
- **Exports** — declare the columns once (`TabelaExportacao<T>`) and both CSV and XLSX come out
  of it. The CSV is UTF-8 **with BOM**, semicolon-separated, comma decimals and `dd/MM/yyyy`
  dates, so a Portuguese Excel opens it correctly; values starting with `=`, `+` or `@` are
  neutralised so a client name can never execute as a formula.
- **Validation** — NIF (with the real check digit), Portuguese plates in all four historic
  formats, postal code and VIN, as both helpers and DataAnnotations attributes.

---

## Security notes

- Cookie authentication, `HttpOnly`, `SameSite=Lax`, `Secure` in production, 10-hour sliding
  expiry; antiforgery tokens on every POST.
- Permissions are resolved **from the database on each check** (30-second cache), not from
  claims baked into the cookie: changing a profile takes effect without forcing users to sign
  in again. The security stamp is revalidated every 5 minutes, so deactivating an account
  ends its session.
- Deactivated accounts are refused by a `SignInManager` override, which covers both sign-in and
  cookie revalidation.
- Lockout after 5 failed attempts for 15 minutes; the failed-login message never reveals
  whether the e-mail exists.
- The `Administrador` profile always holds every permission by design — it is the guarantee
  that no misconfiguration can lock everyone out of the administration screens.
- Deleting is always logical. Records leave the lists but stay in the history, and the
  unique indexes on NIF and matrícula are filtered so a deleted record does not block reuse.
- Export is capped at 20 000 rows; when it truncates, the file says so in its header line
  instead of silently returning a partial list.

---

## What V1 deliberately does not include

Scheduling, quotes, work orders, catalogue and stock are out of scope. Also **not** built,
and worth deciding on before they are needed:

- Accent-insensitive search (`ILIKE` handles case, not accents — needs the `unaccent` extension).
- E-mail sending (password reset by e-mail; today an administrator sets a new password).
- Two-factor authentication (the Identity plumbing is present, no UI).
- Multi-tenancy — one deployment serves one SME.
- Localisation infrastructure: pt-PT strings are written directly in the pages. Adding a second
  language later means moving them into `.resx` files.
