# API endpoints

Base URL defaults to `http://localhost:5080`. Except for auth registration/login/refresh/logout, API routes require a bearer access token. Swagger is exposed at `/swagger` in Development, and `/health` checks SQL Server connectivity.

## Auth and user endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register`, `/login`, `/refresh`, `/logout` | Public (refresh/logout use the HttpOnly refresh cookie) |
| GET | `/api/auth/me` | Authenticated |
| POST | `/api/auth/change-password` | Authenticated |
| GET, PUT | `/api/profile` | Authenticated |
| GET | `/api/users` | Admin |
| PUT | `/api/users/{id}/role` | Admin |
| POST | `/api/users/{id}/deactivate` | Admin |

## CRM routes

Customers, leads, opportunities, tasks, and contacts support `GET collection`, `GET {id}`, `POST`, `PUT {id}`, and `DELETE {id}`. Leads also support `POST /api/leads/{id}/convert`. Notes and activities support collection `GET`, item `GET`, and `POST` (append-only).

All collection routes accept `page` and `pageSize`, clamped to at least 1 and at most 100 items per page. Customer and lead lists support `search`; user list supports `search`; contacts, notes, and activities support relationship filters (`customerId`, and for notes/activities also `leadId` and `opportunityId`). Results use stable server-selected ordering. Client-selectable sort fields/directions are not implemented.

Writes validate payloads and CRM business rules. Sales representatives see and change their assigned records; manager/admin access is broader. A record associated with a customer, lead, or opportunity must also be visible to the caller.

The integration tests cover missing/valid/expired JWT access, SalesRepresentative denial on an Admin route, and invalid input. Real database persistence, database error statuses, and role/ownership interactions against SQL Server remain unverified until a working encrypted SQL connection is available.
