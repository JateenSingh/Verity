# Walkthrough script (~10 minutes)

## 1. Problem framing (1 min)

- iiDENTIFii's partner/engineering discussion currently happens in channels that aren't searchable, structured or auditable, and give no control over moderation — a real problem for a regulated identity-verification business.
- Verity closes that gap: a documented REST API + datastore we own end-to-end, and a web client that consumes that same API — so a partner's own tooling can integrate against it exactly as the browser does.

## 2. Live demo (2 min)

1. `docker compose up --build` from a clean clone → one command, seeded forum at `localhost:4200`.
2. Anonymous browse: filter by date range and author, sort by like count, page through results.
3. Open a post → comments, like count, tags all visible without logging in.
4. Log in as a seeded regular user → create a post, comment on it, like someone else's post (optimistic UI updates instantly, confirmed by the server response).
5. Self-like blocked: try liking your own post → disabled button with a tooltip, and the API itself returns 403 if forced directly.
6. Log in as a seeded moderator → flag a post as misleading/false with a reason, filter the list by that tag, remove the flag.

## 3. Backend decisions (3 min)

- Show `docs/architecture.md`'s layered diagram: Domain has zero framework references; Application depends only on Domain; Infrastructure implements Application's interfaces; Api is the composition root. No MediatR/AutoMapper/generic repository — deliberately light for a 6-hour-shaped API surface.
- Show the SQL in `docs/architecture.md`: `GET /posts` is 2 queries (count + one page, with comment counts and tags folded in via correlated subquery and a joined subquery — not N+1), `GET /posts/{id}` is 1. This is asserted by a real test (`EfficiencyApiTests`) using a `DbCommandInterceptor`, not eyeballed from logs.
- One-like-per-user and one-tag-per-post are enforced by composite primary keys at the database, not just C# checks — demonstrate with the concurrent-like integration test (10 parallel requests, exactly one 201).
- Auth: in-app JWT, `PasswordHasher<T>` (PBKDF2) without the rest of ASP.NET Identity, dummy-hash comparison on unknown-username login so response timing doesn't leak whether an account exists.

## 4. Front-end structure and state (2 min)

- `core/` singletons: typed `ApiClient` mirroring every backend DTO field-for-field, `AuthStore` (signals, localStorage-persisted, discards an expired session), `authInterceptor`/`errorInterceptor` (401 on an authenticated request → logout + redirect to `/login?returnUrl=...`).
- `PostsStore` drives the list off one `filters` signal via `toObservable` + `switchMap`, so a new filter cancels the previous in-flight request — demonstrated by `posts.store.spec.ts`.
- Dumb components (`PostCard`, `LikeButton`, `TagControl`, `CommentForm`) take only `input()`/`output()` and never touch HTTP; the smart pages own the API calls and optimistic state.
- Query-param sync: filters/sort/page round-trip through the URL, so a filtered view is shareable and back/forward works.

## 5. Tests, limitations, and what changes for production (2 min)

- 63 backend tests (25 domain unit + 38 integration against real Postgres via Testcontainers) + 33 frontend Vitest specs = 96 total, all passing.
- Known limitations (see README): JWT in localStorage (XSS surface, mitigated by Angular's default sanitisation — an HttpOnly cookie + CSRF token would be the production move), no refresh tokens (60-minute session), no post/comment edit or delete (explicit scope discipline), moderators only via seed data (no promotion flow), offset paging (fine at this scale, keyset is the production upgrade), no audit log of moderation actions beyond `tagged_by`/`created_at` (the regulatory angle of the problem statement suggests this is the first thing to add).
- Several real bugs were found and fixed by actually running things rather than trusting the code to be right: a JWT claim-remapping issue that silently broke every `CurrentUser.UserId` lookup, an EF retry-strategy/transaction conflict under `EnableRetryOnFailure()`, a rate limiter that wasn't actually partitioned per client, a seed-data week-boundary bug, a `ValidationMessage` component that was `OnPush` but never re-rendered on control state changes, a `.dockerignore` gap that corrupted the containerized build, and a `DateTimeOffset` timezone bug in date-range filtering that only shows up when the server's local timezone isn't UTC.

## Expected questions and one-line answers

- **Why Postgres over SQL Server?** Equal .NET support; Postgres is lighter to self-host and free, matching the "own it end-to-end" goal; the code is provider-agnostic except the snake_case convention and a couple of computed columns.
- **Why controllers over Minimal APIs?** Readability under review and richer built-in versioning/OpenAPI tooling; Minimal APIs would be the right call for a smaller, single-purpose service.
- **Why not full ASP.NET Identity?** Its user store, cookie auth and UI scaffolding aren't justified by this scope; the password hasher alone gives vetted PBKDF2 without reinventing crypto.
- **Why signals over NgRx?** Less ceremony, native to modern Angular; NgRx (or NgRx SignalStore) becomes worth its overhead once several features share genuinely complex cross-cutting state, which this app doesn't have yet.
- **Why denormalise `like_count`?** It's what makes the required like-count sort indexable; the risk of drift is bounded by doing the write inside a transaction with the `likes` table (the actual source of truth) and covered by a concurrency test.
- **What would you change first for production?** The audit-log gap — a regulated forum that lets moderators flag content for compliance reasons needs a durable record of who tagged what and when, beyond what's on the tag row itself.
