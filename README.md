# MiniCRM

MiniCRM is a .NET 8 CRM solution with ASP.NET Core API and MVC hosts, SQL Server persistence through EF Core 8, ASP.NET Core Identity/JWT authentication, and a React/Vite client.

## Implemented

- Shared domain, application service contract, EF Core infrastructure, SQL Server DbContext and Fluent API entity configuration.
- Initial schema and token revocation EF migrations in `Backend/MiniCRM.Infrastructure/Migrations`.
- SQL-backed customers, contacts, leads, opportunities, tasks, notes, activities, ownership scoping, paginated reads, dashboard aggregates, and transactional lead conversion.
- Identity registration/login, Admin/Manager/SalesRepresentative roles, short-lived JWT access tokens, hashed rotating refresh tokens in an HttpOnly cookie, refresh reuse revocation, access-token revocation, password changes, and admin user management.
- API DTO validation, Swagger, structured JSON logs, problem exception handling, health checks, CORS, and rate limits.
- MVC login/registration, live dashboard, customer CRUD, shared-service list and form pages for contacts, leads, opportunities, tasks, notes, and activities, plus profile password change and admin user/role management. Notes and activities are append-only because the application service contract has no update/delete methods for them.
- React authentication, dashboard, customer and CRM module pages, profile and admin user screens.
- xUnit unit and API integration tests, plus Vitest frontend tests.

Sample admin and sales accounts are optional and are created only when `MINICRM_SEED_ADMIN_PASSWORD` and `MINICRM_SEED_SALES_PASSWORD` are configured. Set strong values; seed credentials are not committed.

## Environment note

The NuGet v3 source is `https://api.nuget.org/v3/index.json`. In the development host used to build this workspace, the default Windows SChannel client cannot authenticate outbound TLS to NuGet, while the Node TLS client can. Packages were restored through a temporary local relay that used Node's normal certificate validation; no certificate checks were disabled.

SQL Server diagnostics found the default `MSSQLSERVER` service running on TCP 1433, with `ForceEncryption=0` and no certificate thumbprint configured for the instance. A self-signed `CN=localhost` certificate exists in the machine personal store, but it is not trusted and is not assigned to SQL Server. Separately, `curl.exe` fails TLS to both `www.microsoft.com` and NuGet with `AcquireCredentialsHandle failed: SEC_E_NO_CREDENTIALS`, and SqlClient reports `Encryption not supported on the client`. That points to a host-wide Windows Schannel client-credential problem before the usual server certificate chain check; the NuGet relay worked because Node uses a different TLS stack. The app connection string remains `Encrypt=True;TrustServerCertificate=False`. After Schannel is repaired, the secure SQL setup is a server-auth certificate with a DNS SAN matching the connection name, assigned to SQL Server with private-key read access for the SQL Server service account, and its issuing CA installed in the client trust store. SQL Server certificate requirements are documented by [Microsoft](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/certificate-requirements?view=sql-server-ver17); SqlClient validates the server name when `TrustServerCertificate=False` ([connection string settings](https://learn.microsoft.com/en-us/sql/connect/ado-net/connection-string-syntax?view=sql-server-ver17)). This environment denies access to Schannel and SQL instance configuration details and does not expose the SQL error log, so I could not repair the system TLS provider or assign a certificate. `dotnet ef database update` has not succeeded, and no database schema or SQL-backed CRUD workflow has been verified.

## Configuration and run

Set SQL Server connection string and a random JWT key (at least 32 bytes):

```powershell
$env:ConnectionStrings__DefaultConnection='Server=localhost;Database=MiniCRM;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True'
$env:Jwt__SigningKey='replace-with-a-unique-random-secret-of-at-least-32-bytes'
```

Install the matching EF CLI if it is not already available, then apply migrations on a host that can connect securely to SQL Server:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.32
dotnet ef database update --project Backend/MiniCRM.Infrastructure --startup-project Backend/MiniCRM.API
```

Then run API and MVC (each host runs role initialization; schema migration is explicit):

```powershell
dotnet run --project Backend/MiniCRM.API
```

```powershell
dotnet run --project Backend/MiniCRM.MVC
```

Run the React client:

```powershell
cd Frontend/MiniCRM.React
npm ci
npm run dev
```

Set `VITE_API_URL` to the API base ending in `/api` if the default `http://localhost:5080/api` is not used. In production, serve API and SPA over HTTPS and configure allowed CORS origins. The refresh cookie uses secure production settings.

## Verification in this workspace

- .NET 8.0.428 solution build: passed, zero warnings and errors.
- .NET tests: 9 unit and 5 API integration tests passed.
- React production build: passed.
- Frontend tests: 2 passed.
- `npm audit --audit-level=moderate`: zero vulnerabilities.
- `dotnet ef database update`: blocked by the host's SQL Server encryption/TLS client limitation described above.

## API

Swagger is available in Development at `/swagger`; health endpoint is `/health`. Auth routes include `/api/auth/register`, `/login`, `/refresh`, `/logout`, `/me`, and `/change-password`. Protected CRM routes are under `/api` for customers, leads (including `/{id}/convert`), opportunities, tasks, contacts, notes, and activities. Admin user management is under `/api/users`.

See [architecture](docs/architecture.md), [database design](docs/database-design.md), and [API endpoint notes](docs/api-endpoints.md). No repository was published to GitHub.

