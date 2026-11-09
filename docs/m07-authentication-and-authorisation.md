# M07 - Authentication and authorisation

| | |
|---|---|
| Status | Planned - specified, not built |
| Tag | `v0.7.0` |
| Coverage gate | line >= 90% |
| Boundary | after the interview |

> Lifted from section C.5 of the engineering plan this repository was built from. The plan is the source of truth; this file is the per-milestone view of it.

## Specification

Branch `milestone/m07-auth`. Tag `v0.7.0`. Gate: line >= 90%, auth code >= 95%. Choice: exactly the design D.1 specifies, with no stepping stone — self-issued JWT access token (10 min, D.1.5) signed **ES256** with a JWKS endpoint and key rotation, rotating refresh tokens, and **Argon2id** password hashing via `Isopoh.Cryptography.Argon2` on the OWASP profile with a pepper. D.1.4 rejects PBKDF2 (GPU-friendly, and the Identity default is under-tuned) and D.1.8 rejects HS256 (a shared secret held by every verifier is what makes algorithm confusion exploitable), so neither may appear here: the build plan must not implement what the threat model writes a negative test against.

**7.1** `feat(data): add User entity and unique username index`
- Changes: `User(Id, Username, PasswordHash, CreatedAt, Role)`, unique index on lower-cased username, migration.
- Test: `Duplicate_username_is_rejected_by_database_case_insensitively`. Accept: migration applies on a populated DB.

**7.2** `feat(auth): add POST /auth/register with password hashing`
- Changes: validator (username 3-32 `[a-z0-9_]`, password 12-128), `Isopoh.Cryptography.Argon2` behind an `IPasswordHasher` of our own with D.1.4's parameters (Argon2id, 19 MiB, t=2, p=1) and a pepper from configuration, always `202 Accepted` with an empty body whether or not the username exists (D.1.12 — the build plan must not ship the enumeration oracle the threat model writes a test against). Never logs the password.
- Test: `Register_stores_argon2id_hash_not_password` (asserts the `$argon2id$` prefix and the encoded parameters), `Register_duplicate_returns_the_same_202_as_a_new_user`, `Weak_password_returns_422`, `Hash_verification_is_constant_time_for_unknown_users`.
- Accept: curl register; `select PasswordHash` shows a hash.

**7.3** `feat(auth): add POST /auth/login issuing JWT access tokens`
- Changes: `Microsoft.AspNetCore.Authentication.JwtBearer` with `TokenValidationParameters.ValidAlgorithms = [ES256]` (D.1.8 — never `HS256`, and never an empty algorithm list), a P-256 signing key loaded from the secret store with its `kid` in the header, `GET /.well-known/jwks.json` publishing the public key, claims `sub` (the user's GUID, never the integer key), `role`, `jti`, 10-minute expiry, equal-time failure path (always hash) to blunt user enumeration.
- Test: `Login_returns_es256_token_with_expected_claims`, `Wrong_password_and_unknown_user_return_identical_401`, `Expired_token_is_rejected` (FakeTimeProvider), `Token_signed_HS256_with_the_public_jwk_bytes_is_rejected_401` (D.1.12's algorithm-confusion test), `Token_with_alg_none_is_rejected_401`, `Jwks_endpoint_exposes_only_public_material`.
- Accept: `curl` login; the decoded header shows `alg: ES256` and a `kid` present in `/.well-known/jwks.json`. Decode locally, never by pasting a real token into a website.

**7.4** `feat(api)!: require bearer token on POST, PUT and DELETE /posts`
- Changes: `RequireAuthorization()` on mutating routes, `AuthorId` FK on Posts + migration, `401` problem+json with `WWW-Authenticate: Bearer`. `BREAKING CHANGE` footer.
- Test: `Anonymous_write_returns_401`, `Authenticated_write_succeeds`, `Anonymous_reads_still_200`.
- Accept: curl without/with token. `risk:breaking`, OpenAPI snapshot updated (security scheme).

**7.5** `feat(auth): restrict edit and delete to the post author`
- Changes: resource-based policy (`IAuthorizationHandler`, D.1.11): the author inside the 2 h edit window, or role `moderator`/`admin` with no window and an audit entry; otherwise `403`.
- Test: `Author_can_edit_and_delete_inside_the_window`, `Author_outside_the_two_hour_window_gets_403`, `Other_user_gets_403`, `Moderator_can_edit_any_and_is_audited`, `Admin_can_delete_any`.
- Accept: two users in curl.

**7.6** `feat(auth): add signing-key rotation behind the JWKS endpoint`

- Changes: two keys live at once (`current` for signing, `previous` still served by JWKS until the longest access-token lifetime has passed), rotation every 90 days (D.1.8), `kid` selects the verifier, rotation driven by configuration with no redeploy.
- Test: `Token_signed_with_the_previous_key_still_validates_inside_the_overlap`, `Token_signed_with_a_retired_key_is_rejected`, `Jwks_lists_both_keys_during_overlap`.
- Accept: rotate in a running instance; tokens issued a minute earlier keep working, and a token signed by a key removed from JWKS returns 401.

**7.7** `feat(auth): add rotating refresh tokens with reuse detection`
- Changes: `RefreshToken` table (hash only, never the raw token), `POST /auth/refresh`, `POST /auth/logout`; reuse of a rotated token revokes the whole family.
- Test: `Refresh_rotates_token`, `Reused_refresh_token_revokes_family`, `Logout_invalidates_refresh`.
- Accept: replay an old refresh token: 401 and the new one stops working too.

---

---

Previous: [M06](m06-sorting-and-ranking.md) | Next: [M08](m08-voting.md) | [All milestones](README.md) | [Repository README](../README.md)
