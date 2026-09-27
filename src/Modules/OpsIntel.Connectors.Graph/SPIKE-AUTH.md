# Faz 0 Spike B — public-client PKCE from a Windows service (ADR-0007)

## Question

ADR-0007 flags this as the critical open point: Host is a Windows service (WAM fails there
"by design"), the app must be registered as a **public client** (no client secret, per
ADR-0007/ADR-0008), and the sign-in UX has to be a Host-driven BFF (browser hits Host's own
`/auth/login` and `/auth/callback`, not a native/system browser popup MSAL owns end to end). Can
MSAL.NET do a PKCE authorization-code exchange for a *public* client in that shape?

**Finding: yes.** `AcquireTokenByAuthorizationCode` is confidential-client only (it's a method on
`IConfidentialClientApplication`), so it is not the answer. The answer is MSAL.NET's
`ICustomWebUi` extensibility point (`Microsoft.Identity.Client.Extensibility`), used with
`IPublicClientApplication.AcquireTokenInteractive(scopes).WithCustomWebUi(webUi)`. This is a
supported, documented MSAL.NET seam — it exists specifically so a caller can supply its own
"browser" instead of MSAL's built-in system/embedded browser, while MSAL keeps doing everything
security-sensitive itself:

- MSAL generates the PKCE `code_verifier`/`code_challenge` pair and builds the full authorization
  URL (state, nonce, scopes, redirect_uri, `code_challenge`, `code_challenge_method=S256`).
- MSAL calls `ICustomWebUi.AcquireAuthorizationCodeAsync(authorizationUri, redirectUri, ct)` and
  hands it that URL. **It is this connector's `HostRedirectWebUi` implementation of that one
  method that turns MSAL's "open a browser" step into "redirect this HTTP request".**
- Once `AcquireAuthorizationCodeAsync` returns the callback URL (containing `code` and `state`),
  MSAL parses it, verifies `state`, and does the authorization-code-for-token exchange itself
  against the token endpoint — as a public client, with the `code_verifier` it generated, **no
  client secret involved anywhere**.

So the BFF pattern the ADR describes works with stock MSAL.NET 4.90.1, no forked/unsupported
code path, and no client secret ever exists. This resolves ADR-0007's "Spike B git/gitme" gate:
**git** (proceed with the BFF design), not the WAM/tray fallback.

## The hand-off this connector adds

`ICustomWebUi.AcquireAuthorizationCodeAsync` is a single async call that must (a) *return* the
authorization URL to whoever is driving MSAL, without actually opening any browser itself, and
then (b) *block* until that same caller reports back the browser's redirected callback URL. But
`/auth/login` and `/auth/callback` are two separate, unrelated HTTP requests, potentially minutes
apart, so the two ends of that hand-off don't share a call stack. This connector bridges them
with:

- `GraphLoginSession` (`Auth/GraphLoginSession.cs`) — one per in-flight sign-in, keyed by the
  OAuth `state` value. It holds two `TaskCompletionSource`s: `AuthorizationReady` (the
  authorization URL, set once MSAL builds it) and `CallbackReceived` (the callback URL, set once
  the browser round-trips through Entra and back to `/auth/callback`).
- `HostRedirectWebUi` (`Auth/HostRedirectWebUi.cs`) — the actual `ICustomWebUi`. Its one method
  completes `AuthorizationReady` with the URL MSAL handed it, then awaits `CallbackReceived`.
- `GraphAuthService` (`Auth/GraphAuthService.cs`) — starts `AcquireTokenInteractive(...)
  .WithCustomWebUi(new HostRedirectWebUi(session)).ExecuteAsync()` in the background from
  `BeginInteractiveLoginAsync`, then awaits only `session.AuthorizationReady` before returning (so
  `/auth/login` can redirect the browser immediately). `CompleteInteractiveLoginAsync` (called
  from `/auth/callback`) completes `session.CallbackReceived` and then awaits
  `session.Completion` — the outcome of the whole `ExecuteAsync()` call — so `/auth/callback` can
  report real success/failure rather than just "the redirect was accepted".

`tests/unit/OpsIntel.Connectors.Graph.Tests/Auth/HostRedirectWebUiTests.cs` exercises this
hand-off directly (via `InternalsVisibleTo`), without touching MSAL or the network, since MSAL's
own builder types aren't fake-able without a live authority — that part is Microsoft's own tested
code, not something this spike needed to re-verify.

## Redirect URI

The connector registers a **fixed, path-bearing** redirect URI
(`https://localhost:6500/auth/callback` by default — Host is HTTPS-only, ADR-0003/ADR-0004; the
port is derived from the configured Kestrel port if `GraphAuthOptions.RedirectUri` is left empty,
see `GraphAuthRedirectUriResolver`), not the bare `http://localhost` form ADR-0007 mentions.
Entra's "ignore the port" special case for loopback redirect URIs exists for desktop apps that
spin up a *new, ephemeral* loopback listener
on a random port for each interactive sign-in. Host is the opposite: a single, already-running,
persistent local service listening on a known, configured port. There's nothing to be
port-agnostic about, so this connector just registers the exact URI Host is already listening on.
This still needs the redirect URI registered in Entra under the public-client
("Mobile and desktop applications") platform, which is the only platform type that allows the
`http://` (not `https://`) scheme for `localhost`.

## Token cache

MSAL's token cache (which holds the refresh token) is wired to `ISecretStore`
(`Auth/SecretStoreTokenCache.cs`) via `ITokenCache.SetBeforeAccessAsync`/`SetAfterAccessAsync`
and `SerializeMsalV3()`/`DeserializeMsalV3()`. `ISecretStore`'s Windows implementation
(`DpapiSecretStore`, already in `OpsIntel.Platform.Windows`) DPAPI-protects the blob at rest, so
this connector adds no new "plaintext on disk" surface — it reuses ADR-0011's existing secret
vault rather than introducing MSAL's own file-cache extension package
(`Microsoft.Identity.Client.Extensions.Msal`), which isn't needed here.

## CAE (Continuous Access Evaluation)

`WithClientCapabilities(["cp1"])` is set on the `PublicClientApplicationBuilder`
(`GraphAuthServiceCollectionExtensions.cs`), per ADR-0007's "CAE istemci yeteneği bildirilir".
When a resource issues a claims challenge, `AcquireTokenSilent` throws
`MsalUiRequiredException`; this connector surfaces that as `GraphSignInRequiredException` so the
caller can prompt "sign in again" (ADR-0007's S2 acceptance criterion) rather than retrying
blindly.

## What is *not* validated here (and can't be, in this sandbox)

This spike validates the code path and the extensibility point end to end by construction and by
testing the connector's own coordination logic. It does **not** run an actual Entra sign-in
against a real tenant (no network egress to `login.microsoftonline.com`/`graph.microsoft.com`,
no test tenant available in this environment), so it does not confirm:

- Whether Token Protection, where a tenant enforces it for "all apps", blocks this specific
  registration (ADR-0007 already flags this as an open risk regardless of the auth mechanism
  chosen — Token Protection's supported-app list is Microsoft's own apps only, so a third-party
  WAM-less public client is unlikely to be treated as compliant either way).
- The exact shape of a live claims-challenge response and that `cp1` is sufficient to get the
  28-hour CAE-aware access token lifetime in practice.
- Conditional Access behavior end to end (device compliance, sign-in frequency).

These require a real Entra tenant + test user and are Faz 0's remaining manual validation step
per ADR-0007 ("uyumlu cihaz CA'sı ve Token Protection report-only CA altında test").
