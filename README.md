# Verity

A self-hosted, auditable forum for partner and engineering collaboration, with content moderation and full ownership of the underlying data. It ships as two things that talk to each other over one documented REST API: an ASP.NET Core backend backed by PostgreSQL, and an Angular web client — so a partner's own tooling can integrate against the exact same API the browser uses.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the one-command quick start)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for the developer run / running backend tests locally)
- [Node.js 22 LTS](https://nodejs.org/) and npm (for the developer run / running frontend tests locally)
- [Angular CLI](https://angular.dev/tools/cli) — `npm i -g @angular/cli` (developer run only)
- [Postman](https://www.postman.com/downloads/) (optional, for exercising the API independently of the web client)

## Quick start (one command)

```bash
docker compose up --build
```

Before the first run, copy `.env.example` to `.env` and set `SEED_PASSWORD` to the value shared with you separately - `docker compose up` reads it and refuses to start without it.

```bash
cp .env.example .env
```

This builds and starts Postgres, the API and the web client, applies migrations, and seeds the database automatically on first run.

- Web: <http://localhost:4200>
- API: <http://localhost:5080/api/v1>
- OpenAPI document: <http://localhost:5080/openapi/v1.json>

### Seed accounts

Password for every seeded account is the value you set as `SEED_PASSWORD` (see above) - it is not committed to this repository.

| Username | Role | Notes |
|---|---|---|
| `mod_alice` | Moderator | Demo moderator login |
| `mod_bob` | Moderator | Second moderator, so "tagged by" varies |
| `jateen` | User | Author with the most posts; demo regular login |
| `partner_acme` | User | Represents an integration partner |
| `partner_globex` | User | Second partner |
| `sipho` | User | Regular user |
| `naledi` | User | Zero posts — exercises the empty author-filter case |

## Developer run

Run Postgres in Docker, everything else with your own tooling (faster iteration than rebuilding images):

```bash
docker compose up postgres -d
cd backend && SEED__PASSWORD=<value shared with you separately> dotnet run --project src/Verity.Api
```

In a second terminal:

```bash
cd frontend/verity-web
npm ci
ng serve
```

`ng serve` proxies `/api` to `http://localhost:5080` (see `proxy.conf.json`), so the web client works against the locally-running API with no CORS configuration needed in dev.

### Environment variables

Both the container port (5000) and macOS's AirPlay Receiver collide on many machines, so the API defaults to **5080** everywhere (dev, Docker, Postman) instead.

| Variable | Where | Default (dev) | Purpose |
|---|---|---|---|
| `ConnectionStrings__Verity` | API | `Host=localhost;Port=5432;...` (dev) / `Host=postgres;...` (Docker) | Postgres connection string |
| `Jwt__SigningKey` | API | dev-only value in `appsettings.Development.json` | JWT signing key — **override this for any real deployment** |
| `Jwt__Issuer`, `Jwt__Audience` | API | `verity-api` / `verity-clients` | JWT validation |
| `Cors__AllowedOrigins__0` | API | `http://localhost:4200` | Allowed web-client origin |
| `Seed__Enabled` | API | `true` in Development/Docker | Runs the seeder on startup (idempotent — skips if any user exists) |
| `Seed__Password` | API | *none — required* | Password hashed into every seeded demo account; value shared with you separately, never committed |
| `RateLimiting__Auth__PermitLimit` | API | `10` (falls back to this if unset) | Requests/minute per IP on `/auth/*` |

## Running tests

**Backend** (Docker must be running — the integration tests spin up their own Postgres via Testcontainers; domain tests don't need Docker at all):

```bash
cd backend
dotnet test Verity.sln
```

25 domain unit tests + 38 integration tests, all passing.

**Frontend**:

```bash
cd frontend/verity-web
npm test -- --watch=false
```

33 Vitest specs, all passing.

## API documentation

- **Postman**: import [`docs/postman/Verity.postman_collection.json`](docs/postman/Verity.postman_collection.json) and [`docs/postman/Verity.local.postman_environment.json`](docs/postman/Verity.local.postman_environment.json). Run folders `00`–`05` in order for a full pass (47 assertions); run `06 Rate limit` separately with the Collection Runner set to 11 iterations to see the auth rate limit trip.
- **OpenAPI**: served live at `/openapi/v1.json` whenever the API is running in Development.
- **Versioning**: URL-segment (`/api/v1/...`), via `Asp.Versioning.Mvc`. A `v2` would live alongside `v1` under `/api/v2/...`.
- **Auth in four lines**: `POST /api/v1/auth/register` or `POST /api/v1/auth/login` returns `{ accessToken, tokenType, expiresAt, user }`. Send `Authorization: Bearer <accessToken>` on every subsequent request that needs it. Tokens last 60 minutes; there's no refresh flow yet (see Known limitations). The same flow works identically for the web client and any third-party consumer.
- **Error contract**: every failure is an [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) `application/problem+json` body — `type`, `title`, `status`, `detail`, `instance`, `traceId`, plus an `errors` map for validation failures. See `docs/architecture.md` and the API contract table below for the full status/type mapping.

## Architecture

See [`docs/architecture.md`](docs/architecture.md) for the component diagram, the layered backend structure, the Angular feature structure, real SQL evidence for the query-efficiency requirement, and the full decisions table with justification for each choice (datastore, auth approach, JWT, signals over NgRx, denormalised like count, offset paging).

## Business rules and where they're enforced

| Rule | Enforcement | Failure response |
|---|---|---|
| One like per user per post | Composite primary key `(post_id, user_id)` on `likes` | `409` (`.../already-liked`) |
| No self-like | `Post.EnsureCanBeLikedBy` in the domain layer | `403` (`.../self-like`) |
| Only moderators tag/untag posts | `[Authorize(Policy = "ModeratorOnly")]` | `403` (`401` if anonymous) |
| A post carries a tag at most once | Composite primary key `(post_id, tag)` on `post_tags` | `409` (`.../already-tagged`) |
| Anonymous users may only read | `[Authorize]` on every write endpoint | `401` |
| Registration always creates a regular user | `Role` is never accepted from the request; moderators exist only via seed data | field ignored, not present in the DTO |
| Username/email uniqueness (case-insensitive) | Unique indexes on generated `lower(...)` columns | `409` (`.../username-taken`, `.../email-taken`) |
| Password strength | `PasswordPolicy`: 8–128 chars, at least one letter and one digit | `400` |
| Title/body/comment length | Domain constructor guards + `DataAnnotations` on request DTOs | `400` with an `errors` map |
| Like count never negative | Database `CHECK (like_count >= 0)` + `Math.Max` guard on unlike | covered by tests; would indicate a bug otherwise |
| Auth endpoints rate-limited | Fixed window, 10 req/min per client IP | `429` with `Retry-After` |

## Time allocation

Effort followed the suggested split reasonably closely: the backend API, its domain rules, and its test suite (unit + Testcontainers integration) took up the largest share, consistent with the ~40% backend weighting; the Angular client — core infrastructure, the five main views, and its own test suite — took up most of the rest. Documentation (this README, the architecture notes, the Postman collection) and the walkthrough script were done last, after everything they describe was already working and verified live rather than assumed correct.

## Known limitations and next steps

| Limitation | Why accepted | Production direction |
|---|---|---|
| JWT stored in `localStorage` | Simplest correct option for a SPA plus third-party API in scope; Angular's default sanitisation reduces the XSS surface | `HttpOnly` secure cookie + CSRF token, or a short-lived access token with a rotating refresh token |
| No refresh tokens; session ends at 60 minutes | Out of scope | Rotating refresh tokens, stored hashed, with revocation |
| No 2FA / social login | Explicitly optional in the brief | TOTP via a library like `Otp.NET`, still handled in-app |
| No edit/delete for posts or comments | Not required; scope discipline | Soft-delete with audit fields |
| Moderators only via seed data | No promotion flow was specified | An admin role and an audited promotion endpoint |
| Offset paging | Matches the spec's `page`/`pageSize` shape; fine at this scale | Keyset (cursor) paging on `(created_at, id)` for a much larger forum |
| Denormalised `like_count` could in principle drift | Guarded by a transaction and a concurrency test; `likes` remains the source of truth | A periodic reconciliation job, or a trigger-maintained counter |
| Single tag type (`MisleadingOrFalse`) | The brief names exactly one | A moderator-managed tag lookup table |
| No full-text search | Not required | Postgres `tsvector` with a GIN index |
| Rate limiting is per-process, in-memory | Fine for a single-instance proof of concept | A distributed limiter backed by Redis |
| No audit log of moderation actions beyond `tagged_by`/`created_at` | Timeboxed | An append-only audit table — given the regulatory framing in the problem statement, this is the first thing worth adding |
| HTTPS terminated outside the container | Local run | A reverse proxy / ingress with TLS in front of both services |

## Git approach

`main` and `develop` are both protected by convention (never committed to directly after the initial scaffolding commit) — every change reaches them through a feature branch. Feature branches (`feat/...`, `chore/...`, `test/...`, `docs/...`) are squash-merged into `develop`, with the squash commit's body listing the branch's individual commits so the fine-grained history stays visible on the branch itself. `develop` merges into `main` with a real merge commit (never squashed) at each milestone, tagged `v0.1.0` (backend API), `v0.2.0` (web client), `v0.3.0` (documentation and packaging), and `v1.0.0` (final). Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/).
