# Architecture

The solution separates domain entities and enums (`MiniCRM.Domain`), application contracts and shared business rules (`MiniCRM.Application`), persistence and identity infrastructure (`MiniCRM.Infrastructure`), an HTTP API (`MiniCRM.API`), and a server-rendered Razor application (`MiniCRM.MVC`). Both hosts register the same SQL-backed `ICrmService` implementation. React is a separate Vite client that talks to the API.

`CrmDbContext` derives from ASP.NET Core Identity's EF context and maps CRM records, user roles, refresh tokens, and revoked access tokens to SQL Server. Entity mappings, constraints, indexes, migrations, and a design-time context factory are in Infrastructure. Startup role provisioning is separate from migration application; applying migrations at startup is opt-in through configuration.

Authentication uses Identity, short-lived signed JWTs, hashed rotating refresh tokens in an HttpOnly cookie, a security-stamp claim, and server-side access-token revocation checks. A `CurrentUser` adapter scopes records and linked-record operations. Admin/Manager can access cross-user CRM records; SalesRepresentative operations are limited to owned records.

The MVC and React clients contain module workflows. Notes and activities are append-only in the shared service contract. API validation and exception handling return problem responses, and API middleware provides CORS, rate limiting, health checks, JSON logging, and development Swagger.

## Verification limits

The .NET projects compile and automated unit/API authorization tests run without a database. The local SQL Server cannot currently be reached with TLS because Windows Schannel fails to acquire client credentials for HTTPS generally; database migration, SQL-backed workflows, and full auth/ownership integration require resolving that host TLS issue first. See [database design](database-design.md) and [API endpoint notes](api-endpoints.md).
