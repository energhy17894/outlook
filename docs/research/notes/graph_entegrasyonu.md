# Microsoft Graph / Microsoft 365 Data-Access Integration Patterns for a Locally Installed Windows App (state: late September 2026)

Scope: a Windows-service-based app on the user's PC (plus a local HTTPS web UI on port 6500, no public cloud backend by default) that continuously ingests Outlook/Exchange Online mail, SharePoint/OneDrive documents, optionally Teams meetings/transcripts, Planner/To Do and calendar, and writes back only approval-gated drafts (reply drafts, tasks, calendar items).

Method note: almost all findings below come from Microsoft Learn pages and official Microsoft blogs, fetched directly on 2026-09-27. Where a page shows a "last updated" date, it is given as (upd. YYYY-MM-DD) so the writer can judge freshness. Status labels: **GA** = v1.0 / generally available; **PREVIEW** = beta endpoint or marked preview. The web-search budget ran out partway through, so a few items could only be checked against the Learn/blog pages already reachable. Those items are listed under Gaps.

---

## 1. Authentication for a local desktop + Windows-service app (public vs confidential client, MSAL/WAM, token caching for a service, device code, Conditional Access, app-only)

### Takeaway
Register the app in Entra ID as a **public client (desktop app) using delegated permissions**. Sign-in happens in a **per-user UI/tray component running in the interactive Windows session**, using **MSAL.NET with the Windows broker (WAM)**: interactive sign-in once, silent sign-in after that, with offline_access. **WAM cannot be used from a Windows service by design**, so the service needs a separate token path. Two ways to provide one: (a) the in-session component acquires tokens and hands them to the service, or (b) the service runs as the user and uses a DPAPI-protected MSAL cache without the broker. Avoid device code flow, because Microsoft recommends blocking it. App-only (client credentials with a certificate) is a confidential-client, tenant-wide, admin-consented model. It fits an optional org-deployed backend, not a per-user PC install.

### Cited Findings
- **Public vs confidential client.** MSAL defines public clients as desktop, browserless API and mobile apps, which "can't be trusted to safely keep application secrets, so they can only access web APIs on behalf of the user". Confidential clients are web apps, web APIs and service/daemon apps (upd. 2026-06-15) — [Public client and confidential client applications](https://learn.microsoft.com/en-us/entra/identity-platform/msal-client-applications)
- **What WAM provides.** WAM is the Windows authentication broker, usable by MSAL.NET on Windows 10 1703+ and Windows Server 2019+. MSAL falls back to a browser if WAM can't be used. Benefits listed: Windows Hello, Conditional Access, FIDO keys, the built-in account picker, and "Token Protection. WAM ensures that the refresh tokens are device bound and enables apps to acquire device bound access tokens" (upd. 2025-08-08) — [MSAL.NET WAM](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)
- **Enabling WAM.**
  - Requires MSAL.NET 4.52.0+ and the `Microsoft.Identity.Client.Broker` package, then `.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))`.
  - Requires a parent window handle via `WithParentActivityOrWindow`.
  - Redirect URI `ms-appx-web://microsoft.aad.brokerplugin/{client_id}`, registered under "Mobile and desktop applications".
  - Recommended pattern: `AcquireTokenSilent` with the cached account or `PublicClientApplication.OperatingSystemAccount`, falling back to `AcquireTokenInteractive` on `MsalUiRequiredException`.
  - WAM supports Entra ID only (no B2C or ADFS authorities).
  - Source: [MSAL.NET WAM](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)
- **CRITICAL: WAM does not work from a service.** "When using WAM, your application MUST be running in the context of an active, interactive Windows user session and be able to display UI. Attempting to acquire tokens using WAM while running as a Windows service, using task scheduler (unless specifically running as a logged in user) or while using runas to impersonate another account will result in errors by design." — [MSAL.NET WAM](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)
- **WAM UX guidance.** Give the user context before authentication. Invoke authentication only from a user action. "Explain the benefits of your application if it is a background service." — [MSAL.NET WAM](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)
- **Token cache.** MSAL.NET's cache is in-memory by default. For desktop apps Microsoft recommends the cross-platform cache in `Microsoft.Identity.Client.Extensions.Msal` (`StorageCreationPropertiesBuilder` + `MsalCacheHelper.CreateAsync(...)` + `RegisterCache(pca.UserTokenCache)`), described as "a product-quality, file-based token cache serializer for public client applications (for desktop applications running on Windows, Mac, and Linux)". There is a `WithUnprotectedFile()` plain-text fallback for when encryption at rest fails. The page's sample Windows serializer uses `ProtectedData.Protect/Unprotect(..., DataProtectionScope.CurrentUser)`, i.e. DPAPI tied to the Windows user (upd. 2026-04-29) — [Token cache serialization (MSAL.NET)](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization)
- **Current package versions (NuGet, checked 2026-09-27).** `Microsoft.Identity.Client`, `Microsoft.Identity.Client.Broker` and `Microsoft.Identity.Client.Extensions.Msal` are all at **4.90.1** (published 2026-09-25) — [NuGet Microsoft.Identity.Client](https://www.nuget.org/packages/Microsoft.Identity.Client)
- **Refresh tokens.**
  - Default lifetime is **90 days** for all scenarios except single-page apps and email one-time-passcode flows (24 h).
  - Refresh tokens replace themselves on every use, and the old one is not revoked, so securely delete it.
  - They can be revoked at any time (credential change, user or admin action). Apps must handle revocation by returning to interactive sign-in.
  - Source (upd. 2026-06-15): [Refresh tokens](https://learn.microsoft.com/en-us/entra/identity-platform/refresh-tokens)
- **offline_access.** Delegated OIDC scope "Maintain access to data you have given it access to … even when users are not currently using the app", admin consent not required — [Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
- **Device code flow.** The user has 15 minutes (default `expires_in`) to complete sign-in on another device — [Device authorization grant](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-device-code). Conditional Access can target device code flow. Microsoft calls it "a high-risk authentication method" and says "Microsoft recommends blocking device code flow wherever possible". Protocol tracking means that after a device-code sign-in, later refreshes in the same session stay subject to the authentication-flows policy (upd. 2026-03-25) — [CA: authentication flows](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-authentication-flows)
- **Continuous Access Evaluation (CAE).**
  - Critical events (user disabled or deleted, password reset, MFA enabled, refresh tokens revoked, high user risk) are enforced near real time, with up to 15 minutes of propagation. IP-location enforcement is instant.
  - Exchange Online, SharePoint Online, Teams and MS Graph sync key CA policies.
  - With CAE a resource can reject an unexpired token via a **claims challenge**. "CAE requires a client update to understand claim challenge."
  - In CAE-aware sessions access tokens become long-lived, **up to 28 hours**; without CAE the default is 1 hour.
  - Source (upd. 2026-09-22): [Continuous access evaluation](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-continuous-access-evaluation)
- **Token Protection (Conditional Access session control).**
  - Only device-bound sign-in session tokens (PRT-based) are accepted.
  - **GA on Windows for native apps.** Browser support is preview and limited to web apps that access Azure Resource Manager.
  - Enforceable on Exchange Online, SharePoint Online and Teams.
  - Requires Windows 10+ devices that are Entra joined, hybrid joined or registered.
  - Source (upd. 2026-08-20): [Token Protection](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-token-protection)
- **Token Protection supported apps (Windows guide).** Requires Entra ID P1. The listed supported applications are Microsoft clients only: Outlook, Teams, OneDrive, Word/Excel/PowerPoint, To Do, Microsoft Graph PowerShell with EnableLoginByWAM, Visual Studio with the Windows authentication broker, and others. "Token Protection currently supports native applications only." Unsupported registration types include Entra-joined AVD, Windows 365 Cloud PCs and Autopilot self-deploying (upd. 2026-09-24) — [Token Protection deployment guide – Windows](https://learn.microsoft.com/en-us/entra/identity/conditional-access/deployment-guide-token-protection-windows)
- **App-only permissions.** Application permissions such as Mail.Read, Mail.ReadWrite and Calendars.ReadWrite always require admin consent and apply to **all mailboxes**. "Administrators can configure application access policy to limit app access to specific mailboxes." — [Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference). Scanning apps for SharePoint/OneDrive typically use application permissions (Files.Read.All / Sites.Read.All, and Sites.FullControl.All to process permissions). The grant is tied to tenant + app rather than to the admin — [Scan guidance](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/scan-guidance)

### Inferences
- **Recommended auth topology.**
  - A lightweight **per-user companion process** (tray app, or the web UI's launcher) runs in the interactive session and uses MSAL + WAM for all interactive and silent acquisition. It also exposes a **local, ACL-protected IPC endpoint** (e.g. a named pipe restricted to the service SID) that the background service calls to get a fresh access token.
  - When the user is logged off, ingestion either pauses or falls back to option (b): the service runs **under the user's own account** with a non-broker MSAL `PublicClientApplication` and a `Microsoft.Identity.Client.Extensions.Msal` cache under DPAPI CurrentUser.
  - A LocalSystem service cannot decrypt a CurrentUser-DPAPI cache created in the user's profile. This follows from DPAPI scoping; the page states it only by example.
- **Broker refresh tokens are not in the MSAL cache.** When WAM is the broker, the refresh token is device-bound and held by the broker (the WAM page says refresh tokens are device bound). A non-broker service process therefore probably cannot silently reuse the broker's session. Plan for two token paths, or for the in-session companion only. This should be validated.
- **Handle CAE and Conditional Access.** Implement claims-challenge handling (MSAL "client capabilities", cp1) so the service can recover when Exchange/SharePoint revoke tokens mid-life, and so it benefits from 28-hour tokens.
- **Token Protection risk.** In tenants that enforce Token Protection for "all apps" on Exchange/SharePoint, a third-party app, even one using WAM, is not on Microsoft's supported-app list. It may be blocked unless the policy is scoped or the app is excluded. Recommend customers test in report-only mode.
- **Tenant model.** Use a multi-tenant public-client registration owned by the vendor, or let enterprises register their own (single-tenant) app ID. The latter simplifies admin consent and CA targeting in regulated tenants.
- **Keep app-only out of the default PC install.** A certificate private key with tenant-wide Mail.Read on an end-user PC is a large blast radius. Reserve app-only for an optional customer-hosted backend, scoped with Exchange application access policies (or RBAC for Applications) and Sites.Selected.

### Gaps
- No Microsoft doc was found that describes a supported pattern for sharing WAM-acquired tokens with a Windows service, or for using WAM from Session 0. The WAM page only says it fails by design.
- Not confirmed from a fetched page: whether MSAL.NET writes refresh tokens to its own cache when the broker is enabled (believed not; the broker holds them). The exact `WithClientCapabilities("cp1")` guidance page was not fetched.
- Not documented: whether Token Protection treats third-party WAM-based apps as compliant (the supported list is Microsoft apps only).

---

## 2. Change detection without a public inbound endpoint (webhooks vs Event Hubs vs Event Grid vs Azure Relay vs delta polling; subscription lifetimes; lifecycle and rich notifications)

### Takeaway
Graph delivers change notifications through three documented channels: **webhooks** (need a publicly reachable HTTPS URL), **Azure Event Hubs** (pull via the Event Hubs SDK, no public URL) and **Azure Event Grid partner topics**. A PC-only app with no cloud backend should use **delta-query polling as the baseline**. Event Hubs is an optional enterprise add-on (the customer provisions an Event Hub; the app pulls, then runs delta). Webhooks through Azure Relay are technically possible but fragile for a PC that sleeps. Graph drops notifications to slow endpoints, and driveItem notifications can lag by up to 6 hours, so delta remains the source of truth either way.

### Cited Findings
- **Delivery channels.** Graph can deliver change notifications via Webhooks, Azure Event Hubs and Azure Event Grid. There are three notification types: basic, rich (with resource data) and lifecycle (upd. 2026-04-07) — [Change notifications overview](https://learn.microsoft.com/en-us/graph/change-notifications-overview)
- **Webhook requirements.**
  - "you need to define a publicly accessible HTTPS-secured endpoint".
  - Graph POSTs `?validationToken=...` at creation.
  - A 2xx must arrive **within 3 seconds**, otherwise retries continue for up to 4 hours (10-second timeout on retries).
  - An endpoint is marked "slow" when more than 10% of responses exceed 3 s in a 10-minute window. New notifications are then delayed 10 minutes, and "Dropped notifications can't be recovered."
  - Source (upd. 2026-04-22): [Webhook delivery](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-webhooks)
- **Event Hubs: no public URL needed.** "Webhooks aren't suited … when the receiver can't expose a publicly available notification URL. As an alternative, you can use Azure Event Hubs." You "don't rely on publicly exposed notification URLs. The Event Hubs SDK relays the notifications to your application." You don't need to reply to URL validation; ignore the validation message (upd. 2025-08-29) — [Event Hubs delivery](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-event-hubs)
- **Event Hubs setup.**
  - SAS authentication is **deprecated**; use Entra RBAC by granting the "Microsoft Graph Change Tracking" service principal (appId `0bf30f3b-4a52-48df-9a82-234910c4a086`) the **Azure Event Hubs Data Sender** role.
  - `notificationUrl` is `EventHub:https://<namespace>.servicebus.windows.net/eventhubname/<hub>?tenantId=<domain>` with RBAC, or `EventHub:https://<vault>.vault.azure.net/secrets/<secret>?tenantId=<domain>` with Key Vault.
  - Maximum Event Hubs message size is **1 MB**. Larger rich notifications need a `blobStoreUrl` (container name `microsoft-graph-change-notifications`) and arrive as `additionalPayloadStorageId`.
  - Duplicate `resource` + `changeType` subscriptions return 409.
  - Source: [Event Hubs delivery](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-event-hubs)
- **Event Grid partner topics.**
  - `notificationUrl`/`lifecycleNotificationUrl` = `EventGrid:?azuresubscriptionid=…&resourcegroup=…&partnertopic=…&location=…`. Graph auto-creates the partner topic.
  - Events use CloudEvents format, with retries and dead-lettering.
  - Supported sources: Entra users/groups, Outlook event/message/contact, Teams chatMessage/callRecord, OneDrive driveItem, SharePoint list, To Do task, security alerts, group conversations.
  - Up to 10 partner topics per tenant + app ID.
  - On renewal, `expirationDateTime` must be at least 3 hours ahead.
  - `microsoft.graph.subscriptionReauthorizationRequired` is sent when the access token or subscription is about to expire, or permissions are revoked.
  - The prerequisites list a tenant administrator account (upd. 2026-07-23).
  - Source: [Event Grid – Graph API events](https://learn.microsoft.com/en-us/azure/event-grid/subscribe-to-graph-api-events)
- **Azure Relay Hybrid Connections.** The on-prem listener connects outbound over WebSockets/HTTP. "the on-premises service doesn't need any inbound ports open on the firewall" — [Azure Relay overview](https://learn.microsoft.com/en-us/azure/azure-relay/relay-what-is-it). Relay is **not** listed by Graph as a delivery channel; it would only act as a public HTTPS webhook front-end — [Change notifications overview](https://learn.microsoft.com/en-us/graph/change-notifications-overview)
- **Maximum subscription lifetimes** (upd. 2026-09-17):
  - Outlook message/event/contact: 10,080 min (under 7 days); **with resource data, 1,440 min (under 1 day)**.
  - OneDrive driveItem and SharePoint list: 42,300 min (under 30 days).
  - Teams chat, chatMessage, callTranscript, callRecording, onlineMeeting: 4,320 min (3 days).
  - todoTask: 4,230 min (webhooks global endpoint only).
  - presence: 60 min.
  - Copilot aiInteraction: 4,320 min.
  - Values under 45 min are bumped to 45 min.
  - Source: [subscription resource type](https://learn.microsoft.com/en-us/graph/api/resources/subscription?view=graph-rest-1.0)
- **Notification latency (average / maximum).**
  - message: under 1 min / 3 min.
  - calendar: under 1 min / 3 min.
  - **driveItem and list: under 1 min / 6 hours.**
  - chatMessage: under 10 s / 1 min.
  - callTranscript: under 10 s / 60 min.
  - todoTask: under 2 min / 15 min.
  - event: "Unknown".
  - Source: [subscription resource type](https://learn.microsoft.com/en-us/graph/api/resources/subscription?view=graph-rest-1.0)
- **Lifecycle notifications.**
  - `lifecycleNotificationUrl` is required for Teams resources when expiry is more than 1 h away.
  - reauthorizationRequired: all resources.
  - subscriptionRemoved: Outlook message/event/contact and Teams chatMessage.
  - **missed**: Outlook message/event/contact only.
  - Sources: [Lifecycle notifications](https://learn.microsoft.com/en-us/graph/change-notifications-lifecycle-events); [subscription resource type](https://learn.microsoft.com/en-us/graph/api/resources/subscription?view=graph-rest-1.0)
- **Subscription quotas.** Outlook allows **1,000 active subscriptions per mailbox across all apps**. Teams subscriptions share a **10,000-per-organization** quota, with per-user limits (e.g. 10 per user for all-chats subscriptions) — [Change notifications overview](https://learn.microsoft.com/en-us/graph/change-notifications-overview). Subscription API rate limits: POST/PATCH/DELETE 500 requests per 20 s per app per tenant; GET list 25 per 20 s per app per tenant — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
- **Rich notifications** need `includeResourceData: true`, plus `encryptionCertificate` and `encryptionCertificateId`; the payload is encrypted to the app's public key. Outlook message/event rich notifications require `$select` (upd. 2026-04-17) — [Rich notifications](https://learn.microsoft.com/en-us/graph/change-notifications-with-resource-data)
- **Push + delta combination.** Microsoft recommends combining notifications with delta: run delta right after subscribing, and keep a periodic delta check "no more than once per day" as a safety net when webhooks are used. Drive subscriptions support only the `updated` change type on the drive root — [Scan guidance](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/scan-guidance); [Delta query overview](https://learn.microsoft.com/en-us/graph/delta-query-overview)

### Inferences
- **Recommended default for a local app: delta polling only.** Suggested intervals:
  - Mail folders every 1–2 min for Inbox and Sent, 10–15 min for other folders.
  - Calendar view every 5 min.
  - OneDrive and each tracked SharePoint drive every 5–15 min.
  - Teams transcripts on meeting end + 15 min, then hourly.
  - At these rates the budget stays far below the Outlook limit of 10,000 requests per 10 minutes per app per mailbox (see §4).
  - Wake-up from sleep: resume from the stored deltaLink. If the token has expired, full resync.
- **Optional "near-real-time" mode for enterprise customers.** The customer provisions an Event Hub (RBAC, Data Sender role for Graph Change Tracking) and grants the local service Listen rights with a Data Receiver role or connection string. The service opens an **outbound** AMQP/WebSocket consumer. Each notification only triggers an immediate delta call; no content is trusted from the notification.
- **Use basic notifications, not rich ones.** This avoids certificate management on the PC and keeps Outlook subscription lifetimes at about 7 days rather than 1 day. The app must renew subscriptions on a timer, e.g. every 3 days for Outlook, and handle reauthorizationRequired. That requires a working delegated token, which ties back to the auth design in §1.
- **Why not webhooks via Azure Relay / dev tunnels.** The 3-second response SLA, drop-on-slow behavior and PC sleep/offline make this an anti-pattern for a laptop. Event Hubs retains messages while the PC is offline, within the hub's retention setting.
- **Event Grid partner topics** are better suited to an org-hosted backend. They require Azure resource-provider registration and partner authorization by an admin, and delivery still needs an Event Grid handler, e.g. to a Service Bus/Storage queue that the PC then polls. That is more moving parts than Event Hubs for this use case.

### Gaps
- Not confirmed: whether the Event Hub may live in a different Entra tenant (e.g. a vendor tenant) from the M365 data tenant. The doc says the `tenantId` domain "must match the domain used by the Azure subscription that holds the Azure Event Hubs", which is ambiguous for cross-tenant setups.
- No official guidance was found on subscription behavior when the PC is offline for longer than the subscription lifetime, beyond "create a new subscription".
- Event Grid pull delivery (namespace topics) combined with Graph partner topics was not verified.

---

## 3. Delta query details (mail folders/messages, driveItems incl. SharePoint libraries and shared/followed sites, calendar events; token storage; 410 Gone / resync; immutable IDs)

### Takeaway
Delta is **per collection**:
- per **mail folder** (`/me/mailFolders/{id}/messages/delta`);
- per **drive** (`/drives/{id}/root/delta`);
- per **calendarView window** (`/me/calendarView/delta?startDateTime&endDateTime`). A non-windowed events delta is beta only.

Store the full `@odata.deltaLink` URL as opaque state. Expect replays and duplicates. Handle 410 Gone and `syncStateNotFound` with a full resync. Always send `Prefer: IdType="ImmutableId"` for Outlook items. Note that **`/me/drive/sharedWithMe` is deprecated and stops returning data in November 2026**, so discovery of shared or followed content must use other APIs.

### Cited Findings
- **General delta behavior** (upd. 2026-05-14): [Delta query overview](https://learn.microsoft.com/en-us/graph/delta-query-overview)
  - Continue paging through `@odata.nextLink` until `@odata.deltaLink` appears. A page never contains both.
  - Tokens encode the original query parameters, so don't repeat `$select`.
  - Deletions arrive as `@removed` with reason `changed` (restorable) or `deleted`.
  - Replays are possible, and the same entity can appear multiple times.
  - Supported resources include message, mailFolder, event, contact, driveItem, listItem, sites, todoTask, todoTaskList, chatMessage, **callTranscript**, **callRecording**, mailboxFolder and mailboxItem, plus plannerBucket (beta).
- **Resync and token expiry** — [Delta query overview](https://learn.microsoft.com/en-us/graph/delta-query-overview)
  - A delta call can return **410 Gone** with a `Location` header holding an empty `$deltatoken`, meaning a full resync is required.
  - For Outlook entities (message, mailFolder, event, contact, todoTask, todoTaskList) token lifetime is "not fixed; it's dependent on the size of the internal delta token cache". Expired tokens produce a 40X error such as `syncStateNotFound`.
  - Directory objects: 7 days.
- **Message delta** (upd. 2026-06-19): [message: delta](https://learn.microsoft.com/en-us/graph/api/message-delta?view=graph-rest-1.0); [Delta for messages](https://learn.microsoft.com/en-us/graph/delta-query-messages)
  - Least-privileged delegated permission is **Mail.ReadBasic** (Mail.Read is needed for bodies).
  - Supports `$select`, `$top`, `$expand`, and `changeType=created|updated|deleted`.
  - `$filter` is limited to `receivedDateTime ge|gt` and "returns only up to 5,000 messages". `$orderby` is limited to `receivedDateTime desc`. There is no `$search`.
  - `Prefer: odata.maxpagesize`.
  - Delete/move and read-state events are emitted even when they don't match the filter.
  - "Delta query is a per-folder operation … you need to track each folder individually."
- **driveItem delta** (upd. 2026-06-06): [driveItem: delta](https://learn.microsoft.com/en-us/graph/api/driveitem-delta?view=graph-rest-1.0)
  - Endpoints: `/drives/{drive-id}/root/delta`, `/sites/{siteId}/drive/root/delta`, `/me/drive/root/delta`, `/groups/{id}/drive/root/delta`. Delegated least privilege is Files.Read; Files.Read.All or Sites.Read.All are needed for other users' or sites' drives.
  - `?token=latest` returns only the current deltaLink ("sync from now").
  - The feed shows the latest state per item. The same item can appear more than once (use the last occurrence). `parentReference.path` is not returned.
  - Headers: `deltaExcludeParent`, `Prefer: hierarchicalsharing`, and `Prefer: deltashowremovedasdeleted, deltatraversepermissiongaps, deltashowsharingchanges` (the permission-change flag requires Sites.FullControl.All).
  - **410 Gone** comes with `resyncChangesApplyDifferences` or `resyncChangesUploadDifferences` and a Location with a new enumeration link.
- **SharePoint coverage** — [Scan guidance](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/scan-guidance)
  - SharePoint sites can have multiple drives, one per document library; discover them with `/sites/{id}/drives`.
  - Use delta for the initial crawl, because paging children "is not guaranteed to return every single item".
  - Site enumeration delta is rolling out on beta.
- **Followed and shared content.**
  - `GET /me/followedSites` is delegated only (Sites.Read.All), "has a known issue and might return incorrect results", and works through OneDrive for Business only — [List followed sites](https://learn.microsoft.com/en-us/graph/api/sites-list-followed?view=graph-rest-1.0)
  - **`drive: sharedWithMe` (deprecated)**: "deprecated and will operate in a degraded state until November, 2026, after which it will stop returning data" (upd. 2026-03-21) — [drive: sharedWithMe](https://learn.microsoft.com/en-us/graph/api/drive-sharedwithme?view=graph-rest-1.0)
- **Calendar delta** (upd. 2026-05-14): [event: delta](https://learn.microsoft.com/en-us/graph/api/event-delta?view=graph-rest-1.0); [Delta for events](https://learn.microsoft.com/en-us/graph/delta-query-events)
  - v1.0 is `GET /me/calendarView/delta?startDateTime=…&endDateTime=…`, tracked per calendar and per window. Delegated least privilege is Calendars.Read.
  - `$select`, `$expand`, `$filter`, `$orderby` and `$search` are **not supported**.
  - Delta on a calendar without a fixed range is **beta only**.
- **Immutable IDs** (upd. 2025-08-06): [Immutable IDs](https://learn.microsoft.com/en-us/graph/outlook-immutable-id)
  - `Prefer: IdType="ImmutableId"` must be sent on **every** request.
  - IDs stay stable across folder moves, but change on moves to the archive mailbox or on export/re-import.
  - Supported on message, attachment, event, eventMessage, contact and outlookTask. Containers (mailFolder, calendar) already have stable IDs.
  - Works with delta: deltaLinks are compatible with both ID formats, so no resync is needed.
  - Subscriptions must be recreated with the header.
  - `translateExchangeIds` converts up to 1,000 IDs per call.
  - Use case: create a draft with an immutable ID, send it, then find the Sent Items copy by the same ID.
- **Transcripts delta.** `getAllTranscripts` supports delta but is **application-permission only** (OnlineMeetingTranscript.Read.All, delegated "Not supported"). A tenant admin must enable Graph API access to meeting transcripts (upd. 2026-08-21) — [onlineMeeting: getAllTranscripts](https://learn.microsoft.com/en-us/graph/api/onlinemeeting-getalltranscripts?view=graph-rest-1.0)

### Inferences
- **Suggested local state table.** One row per (resource-type, container-id), holding `deltaLink`, `lastSuccessUtc`, `lastFullSyncUtc`, `errorCount` and `idType=immutable`.
- **Per-row processing.** Process pages idempotently (upsert by immutable ID or driveItem ID, last-write-wins). On 410/`syncStateNotFound`, start a full resync from the `Location` URL, then reconcile deletions by set difference.
- **Mail scope.** Enumerate `mailFolders` (with `childFolders`) and track each folder, or limit tracking to Inbox, Sent Items and user-chosen folders to save requests. Use `$select` to reduce payload, and fetch bodies and attachments lazily.
- **Calendar scope.** Use a rolling calendarView window, e.g. −30 days to +180 days, and recreate the delta chain when the window slides (the window is encoded in the token).
- **Shared content after November 2026.**
  - Build discovery from sites the user explicitly selects, plus `/me/followedSites` (with its caveats).
  - Add Microsoft Search `/search/query` over driveItem/listItem for recently used or shared documents.
  - Add Teams/Group drives (`/me/joinedTeams` → group drive).
  - Then run delta per discovered drive.

### Gaps
- No official statement was found on typical Outlook delta token lifetime in days (the doc says "not fixed").
- No fetched page confirmed a replacement API for `sharedWithMe`, or the status of `/me/insights/shared`.

---

## 4. Throttling and rate limits for one user or mailbox (Outlook, SharePoint/OneDrive resource units, Teams, batching, Retry-After, 2025–2026 changes)

### Takeaway
For a single user the binding limits are:
- **Outlook, per app + mailbox:** 10,000 requests per 10 minutes, **4 concurrent requests**, 150 MB of uploads per 5 minutes.
- **SharePoint/OneDrive, per user:** 3,000 requests per 5 minutes.
- **SharePoint/OneDrive, per app per tenant:** resource-unit budgets. Delta with a token costs 1 RU.
- **Teams:** per-user or per-chat rates as low as 1 request/second.

JSON batching allows 20 requests per batch but does not bypass per-request limits. Always honor `Retry-After`, and note that **SharePoint does not return IETF RateLimit headers** (a change from older guidance).

### Cited Findings
- **Outlook limits per mailbox** (upd. 2026-09-17): [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
  - Applied per app ID + mailbox: "10,000 API requests in a 10-minute period", "Four concurrent requests", and "150 megabytes (MB) upload (PATCH, POST, PUT) in a 5-minute period" (v1.0 and beta).
  - Covers the Mail, Calendar, personal contacts, To Do (outlookTask) and mailbox import/export APIs.
  - For unordered JSON batches, Graph sends at most 4 requests at a time to Outlook. `dependsOn` serializes the batch, and up to four such batches can run concurrently.
- **Global limit.** 130,000 requests per 10 seconds per app across all tenants — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
- **Insights and people.** me/insights and people: 10,000 requests per 10 min and 4 concurrent — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
- **Planner.** "Service limits for Planner aren't available." — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
- **Teams limits** (per app / per app per tenant / per resource / per user): [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
  - `GET /me/chats` 200 / 20 rps, with **1 rps per user**.
  - `GET /chats/{id}/messages` 200 / 20 rps, with **1 rps per chat**.
  - `GET /teams/{id}/channels/{id}/messages` 200 / 20 rps, 1 rps per channel.
  - A sustained limit of about 83% of the listed value applies over longer windows.
- **Cloud communications.** Meeting information is limited to 2,000 meetings per user each month — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)
- **SharePoint/OneDrive** (upd. 2026-08-10): [Avoid throttling in SharePoint Online](https://learn.microsoft.com/en-us/sharepoint/dev/general-development/how-to-avoid-getting-throttled-or-blocked-in-sharepoint-online)
  - Throttled requests get **429 or 503 with Retry-After**. Persistent abuse can lead to a block (503) announced in Message Center.
  - Resource-unit costs: 1 RU for a single-item GET, **delta with a token**, or a file download; 2 RU for a multi-item query or create/update/delete/upload; 5 RU for any permission operation or `$expand=permissions`.
  - **User limits:** 3,000 requests per 5 min, 50 GB ingress and 100 GB egress per hour.
  - **Per app per tenant limits:** 1,250 RU/min and 1,200,000 RU/24 h for tenants with 0–1,000 licenses, scaling up to 6,250 RU/min and 6,000,000 RU/24 h above 50,000.
  - Tenant-wide: "Assign Sensitivity Label" is limited to 100 per 5 min.
  - Batched requests are evaluated individually by RU.
  - Decorate traffic with the User-Agent format `ISV|CompanyName|AppName/Version`.
  - "SharePoint Online does not return or support IETF RateLimit headers."
- **Batching** (upd. 2025-02-26). "JSON batch requests are currently limited to 20 individual requests." Each request is throttle-evaluated individually, and a batch can return 200 with inner 429s — [JSON batching](https://learn.microsoft.com/en-us/graph/json-batching)
- **Retry guidance** (upd. 2025-08-06): [Throttling guidance](https://learn.microsoft.com/en-us/graph/throttling)
  - On 429, wait `Retry-After` seconds. If no header is present, use exponential backoff.
  - Throttled requests still count against usage.
  - The Graph SDKs implement Retry-After and backoff handlers.
  - "continuously polling a resource … are more likely to lead to applications being throttled"; use change tracking.
  - Bulk extraction should use Microsoft Graph Data Connect.
- **Where Retry-After is missing.** Some identity/CA-policy resources don't return Retry-After on 429 — [Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)

### Inferences
- **Budget math for one mailbox** (10,000 requests per 10 minutes):
  - Polling 5 folders every 60 s plus a calendarView every 5 min is about 52 requests per 10 minutes.
  - Body and attachment fetches for, say, 300 new mails per day is about 600 requests per day.
  - So there is about 99% headroom. The real constraint is **4 concurrent requests per mailbox**, so use a per-mailbox semaphore of 3–4 and serialize write-backs.
- **SharePoint scheduling.** Delta-with-token at 1 RU makes frequent polling cheap. However, several users of the same product in one tenant share the app's per-tenant RU bucket; e.g. 100 PCs × 10 drives × 1 poll/5 min ≈ 200 RU/min, well under 1,250 RU/min for small tenants. Include backoff with jitter and off-peak scheduling for initial full crawls.
- **Retry policy.** Implement a single retry handler (the SDK's RetryHandler plus a custom circuit breaker per workload) that honors Retry-After on 429 and 503, including inner batch responses.

### Gaps
- No 2025–2026 change to the Outlook 10,000/10-min or 4-concurrent limits was found. An older third-party claim about higher limits was not verified.
- No published limits were found for the Microsoft Search API beyond the Outlook bucket it shares, nor for the To Do (todoTask) v1.0 API separately.

---

## 5. Delegated permission scopes, least privilege, admin consent and user-consent restrictions

### Takeaway
The per-scope admin-consent flags still say "user-consentable" for most delegated mail, calendar and file scopes. However, Microsoft's **managed default user-consent policy** ("Let Microsoft manage your consent settings", the default for new tenants) **blocks user consent for Mail.*, Calendars.*, Files.Read.All, Sites.Read.All, Chat.Read, Tasks.*, People.Read and more**. In practice, plan for **admin consent in essentially every enterprise tenant**, and ship an admin-consent URL plus admin-consent-workflow guidance. Teams transcripts, AI insights and label scopes require admin consent regardless.

### Cited Findings
- **Per-scope delegated admin-consent flags** (upd. 2026-09-15), [Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference):
  - **No admin consent needed:** User.Read, offline_access, Mail.ReadBasic, Mail.Read, Mail.ReadWrite ("does not include permission to send mail"), Mail.ReadWrite.Shared, Mail.Send, Calendars.Read, Calendars.ReadWrite, MailboxSettings.ReadWrite, Files.Read, Files.Read.All, Sites.Read.All, Sites.Selected (delegated), Tasks.ReadWrite, Chat.Read, People.Read, OnlineMeetings.Read, InformationProtectionPolicy.Read.
  - **Admin consent required (Yes):** OnlineMeetingTranscript.Read.All, CallTranscripts.Read.All, OnlineMeetingAiInsight.Read.All, ChannelMessage.Read.All, Group.Read.All, SensitivityLabels.Read.All, Files.SelectedOperations.Selected.
  - Files.Read.Selected (preview): "Read files that the user selects".
- **Microsoft-managed default consent policy** (upd. 2026-08-28): [App consent policies](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/manage-app-consent-policies)
  - The policy `microsoft-user-default-recommended` backs "Let Microsoft manage your consent settings". It "will update with Microsoft's latest recommended default consent settings. This is also the default for a new tenant."
  - Users can consent to any user-consentable delegated permission **EXCEPT**: Files.Read.All, Files.ReadWrite.All, Sites.Read.All, Sites.ReadWrite.All, Mail.Read, Mail.ReadWrite, Mail.ReadBasic, Mail.*.Shared, MailboxItem.Read, Calendars.Read, Calendars.ReadBasic, Calendars.ReadWrite, Calendars.*.Shared, Chat.Read, Chat.ReadWrite, OnlineMeetings.Read/ReadWrite, MailBoxFolder.*, MailBoxSettings.*, Contacts.ReadWrite, Contacts.*.Shared, Tasks.Read, Tasks.ReadWrite, Tasks.*.Shared, People.Read. The same applies to Exchange EAS/EWS/IMAP/POP.AccessAsUser.All.
  - A separate `microsoft-user-default-allow-consent-apps` policy lets users consent to mail scopes only for named mail clients (Apple Mail, Spark, eM Client, Thunderbird, Android mail).
- **Other built-in consent policies** (upd. 2026-08-04): `microsoft-user-default-low` (verified publishers plus admin-classified low-impact permissions) and `microsoft-user-default-legacy` (any permission not requiring admin consent). Microsoft recommends allowing user consent only for verified publishers — [Configure user consent](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/configure-user-consent)
- **Admin consent workflow.** Users can request consent. Requests go by email to designated reviewers, who need the rights to grant consent. Global Admin is required to turn the workflow on (upd. 2026-02-19) — [Admin consent workflow](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/configure-admin-consent-workflow)
- **Least-privilege examples from API pages.**
  - Message delta: Mail.ReadBasic — [message: delta](https://learn.microsoft.com/en-us/graph/api/message-delta?view=graph-rest-1.0)
  - driveItem delta: Files.Read — [driveItem: delta](https://learn.microsoft.com/en-us/graph/api/driveitem-delta?view=graph-rest-1.0)
  - calendarView delta: Calendars.Read — [event: delta](https://learn.microsoft.com/en-us/graph/api/event-delta?view=graph-rest-1.0)
  - createReply: Mail.ReadWrite — [message: createReply](https://learn.microsoft.com/en-us/graph/api/message-createreply?view=graph-rest-1.0)
  - Create To Do task: Tasks.ReadWrite (application not supported) — [Create todoTask](https://learn.microsoft.com/en-us/graph/api/todotasklist-post-tasks?view=graph-rest-1.0)
  - Create Planner task: Tasks.ReadWrite (higher: Group.ReadWrite.All; application Tasks.ReadWrite.All) — [Create plannerTask](https://learn.microsoft.com/en-us/graph/api/planner-post-tasks?view=graph-rest-1.0)
  - `/search/query`: delegated least privilege Mail.Read; application Files.Read.All — [search: query](https://learn.microsoft.com/en-us/graph/api/search-query?view=graph-rest-1.0)
- **Teams transcripts, delegated.**
  - `GET /me/onlineMeetings/{id}/transcripts` accepts delegated **OnlineMeetingTranscript.Read.All** (admin consent). It is access-governed by a tenant-admin setting and works only for meetings associated with a calendar event that have not expired (upd. 2026-06-29) — [List transcripts](https://learn.microsoft.com/en-us/graph/api/onlinemeeting-list-transcripts?view=graph-rest-1.0)
  - Two independent admin settings apply. "Graph API access to transcripts": when disabled, requests return 403 `GraphAccessToTranscriptsDisabled`. "Speaker attribution" is the second. App access uses organization-wide application permissions or meeting-scoped RSC (upd. 2026-08-03) — [Transcripts overview](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/overview-transcripts)
- **Metering: conflicting documentation.**
  - The transcripts overview still says "The APIs to fetch meeting transcripts and recordings are metered APIs" — [Transcripts overview](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/overview-transcripts)
  - The Teams licensing page says: "Starting August 25, 2025, the Teams APIs listed in this article are no longer metered". Its only exceptions are AI insights (Copilot license) and DLP PATCH, and the page itself is marked deprecated. It still lists transcript content at $0.0022/min and recording at $0.003/min, with evaluation mode returning 402 once exhausted — [Teams API payment models](https://learn.microsoft.com/en-us/graph/teams-licenses)

### Inferences
- **Minimum delegated scope set (core):** `User.Read offline_access Mail.ReadWrite Calendars.ReadWrite Files.Read.All Sites.Read.All Tasks.ReadWrite MailboxSettings.Read`.
  - Mail.ReadWrite covers read + drafts + categories/flags; there is no need for Mail.Read separately.
  - Add `MailboxSettings.ReadWrite` only if the app creates new master categories.
  - Add `People.Read` for contact ranking.
- **Optional add-ons:** `OnlineMeetings.Read` + `OnlineMeetingTranscript.Read.All` (admin), `Chat.Read`, `OnlineMeetingAiInsight.Read.All` (admin + Copilot license).
- **Do not request `Mail.Send`.** Drafts are created with Mail.ReadWrite, and the user sends from Outlook. This keeps the "human executes" guarantee cryptographically enforced by the token.
- **Use incremental consent.** Request optional scopes only when the user enables the module. Ship an admin-consent link (`/adminconsent`) and publisher verification. Most enterprise tenants will be on managed or restricted consent.
- **Least privilege via Sites.Selected.** For customers who want to restrict SharePoint access, offer a mode using delegated **Sites.Selected** (user-consentable per the flag, but per-site grants are configured by admins in SharePoint) instead of Sites.Read.All.

### Gaps
- The date Microsoft switched existing tenants to the managed policy, and the share of tenants on it, were not verified (the web-search budget was exhausted).
- The metering status of transcript content downloads as of September 2026 is conflicting (see above). Verify with Microsoft or test for 402 responses.

---

## 6. Microsoft 365 Copilot APIs, Microsoft Search API, and Microsoft MCP servers (Enterprise MCP, Work IQ MCP, Agent 365)

### Takeaway
- **Copilot APIs** (Retrieval, Meeting AI Insights, Chat, Search) are Graph endpoints under `/copilot`, **delegated**, and require a **Microsoft 365 Copilot license per user**.
  - The **Retrieval API** has a **v1.0 endpoint** (SharePoint, OneDrive, connectors; 200 requests per user per hour). A pay-as-you-go option is **preview** and covers SharePoint and connectors only.
  - The **Chat** and **Search** APIs are **preview**.
  - **Meeting AI Insights** has v1.0 and requires Copilot licensing.
- The **Microsoft Search API** (`/search/query`) is GA, needs no Copilot license, and searches mail, events and files.
- **MCP servers:**
  - The **Microsoft MCP Server for Enterprise** is preview and **Entra directory read-only**. It is not for mail or files.
  - **Work IQ MCP** (a remote server plus a local `@microsoft/workiq` CLI MCP) exposes mail, calendar, files and Teams via 10 generic tools. It is preview, requires a Copilot license and admin consent, and blocks mutations by default.
- These are useful as optional accelerators for Copilot-licensed tenants, not as the ingestion backbone.

### Cited Findings
- **Copilot APIs overview** (upd. 2026-08-11): [Copilot APIs overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/copilot-apis-overview)
  - Lists the Retrieval API, Search API (preview), Interaction Export API, AI Interactions Change Notifications (preview), Meeting Insights API, AI Insights Change Notifications (preview), Chat API (preview), usage reports and package management.
  - Endpoints are `graph.microsoft.com/v1.0/copilot` and `/beta/copilot`.
  - Requirements: "Microsoft 365 Copilot license – Required for each user who accesses Microsoft 365 Copilot functionality via these APIs" plus E3/E5 or equivalent.
  - Use is under the "Microsoft 365 Copilot APIs Terms of Use (preview)".
- **Security model** (upd. 2026-07-16): The Retrieval API "supports delegated permissions only". Conditional Access, sensitivity labels and permission trimming are enforced automatically — [Copilot APIs security & authentication](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/copilot-apis-security-authentication)
- **Retrieval API reference** (upd. 2026-08-31): [copilotRoot: retrieval](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval)
  - `POST https://graph.microsoft.com/v1.0/copilot/retrieval` (also beta).
  - Delegated: Files.Read.All **and** Sites.Read.All for SharePoint/OneDrive; ExternalItem.Read.All for connectors. Application: not supported.
- **Retrieval API limits** (upd. 2026-08-20): [Retrieval API overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview)
  - queryString up to 1,500 characters.
  - One dataSource per call.
  - maximumNumberOfResults up to 25.
  - **200 requests per user per hour**.
  - Semantic/hybrid retrieval only for .doc/.docx/.pptx/.pdf/.aspx/.one; other file types lexical only.
  - No images or charts.
  - Free with the Copilot add-on license; otherwise PAYG preview.
- **Retrieval pay-as-you-go (preview).** Tenant-level sources only (SharePoint, connectors; **no OneDrive**). Requires at least one Copilot license in the tenant and an Azure subscription. **$0.10 per API call** (Copilot Credit meter). Enabled in the M365 admin center under Copilot > Billing & usage > Pay-as-you-go. No SLA (upd. 2026-07-02) — [Retrieval PAYG](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/paygo-retrieval). Announced January 2026 — [What's new](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/whats-new)
- **Copilot Search API (PREVIEW / beta).** Hybrid semantic + lexical search over **OneDrive for work or school only**. "APIs under the /beta version are subject to change. Use of these APIs in production applications is not supported." (upd. 2026-03-24) — [Copilot Search API](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/search/overview)
- **Copilot Chat API (PREVIEW).** Multi-turn chat with enterprise and web grounding, synchronous or SSE streaming. No actions: it can't create files, send emails or schedule meetings. Text-only. No long-running tasks. Copilot license required; no support for unlicensed users (upd. 2026-07-02) — [Chat API](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/chat/overview)
- **Meeting AI Insights.**
  - Summaries, action items and mentions from transcribed meetings. "You can only fetch insights on behalf of a Microsoft 365 Copilot licensed user." Transcription or recording must be on. Channel meetings are not supported (upd. 2026-07-20) — [Meeting AI Insights](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/meeting-insights)
  - `GET https://graph.microsoft.com/v1.0/copilot/users/{userId}/onlineMeetings/{onlineMeetingId}/aiInsights`. Delegated OnlineMeetingAiInsight.Read.All; application requires an application access policy — [List aiInsights](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/meeting-insights/onlinemeeting-list-aiinsights)
- **Microsoft Search API (GA).** A unified endpoint over Outlook messages and events, SharePoint/OneDrive driveItem, list, listItem, site and drive, person, externalItem (connectors) and admin answers — [Search API overview](https://learn.microsoft.com/en-us/graph/search-concept-overview). `POST /search/query`: delegated least privilege Mail.Read; application Files.Read.All/Sites.Read.All (files) — [search: query](https://learn.microsoft.com/en-us/graph/api/search-query?view=graph-rest-1.0)
- **Microsoft MCP Server for Enterprise (PREVIEW)** (upd. 2026-07-04): [MCP Server for Enterprise](https://learn.microsoft.com/en-us/graph/mcp-server/overview)
  - Endpoint `https://mcp.svc.cloud.microsoft/enterprise`.
  - Tools: `microsoft_graph_suggest_queries`, `microsoft_graph_get`, `microsoft_graph_list_properties`.
  - "focuses on Microsoft Entra identity and directory read-only scenarios".
  - No extra cost; 100 calls per minute per user; global cloud only; app ID `e8c77dc2-69b3-43f4-bc51-3213c9d915b4` for log filtering.
- **Work IQ MCP** (upd. 2026-08-21): [Work IQ MCP overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/mcp/overview); [Work IQ MCP tool reference](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/mcp/tool-reference)
  - A single MCP endpoint with 10 tools: `fetch`, `create_entity`, `update_entity`, `delete_entity`, `do_action`, `call_function`, `ask`, `list_agents`, `get_schema`, `search_paths`.
  - Tools operate on Graph-like relative paths, e.g. `fetch /me/messages` or `call_function /search/query`.
  - "Policy over scopes": four broad OAuth permissions, with per-path tenant policy. The tenant policy "blocks mutation operations by default".
  - Auth is discovered via `/.well-known/oauth-protected-resource`.
- **Agent 365 / Work IQ MCP servers (PREVIEW)** (upd. 2026-08-13): [Agent 365 tooling servers](https://learn.microsoft.com/en-us/microsoft-agent-365/tooling-servers-overview)
  - "You must have a Microsoft 365 Copilot license to use Work IQ MCP servers."
  - Listed clients: M365 admin center, Copilot Studio, Microsoft Foundry.
  - Catalog: Work IQ Copilot, Calendar, Mail, SharePoint, OneDrive, Teams, User, Word; admins allow or block servers.
- **Work IQ API permission.** App ID URI `api://workiq.svc.cloud.microsoft`, delegated scope `WorkIQAgent.Ask` with admin consent required. An org admin must enable Work IQ (upd. 2026-06-17) — [Work IQ permissions](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/permissions). Enablement needs a Copilot Studio usage-based billing plan (Copilot Credits) and a Global Admin creating the service principal `fdcc1f02-fc51-4226-8753-f668596af7f7` (upd. 2026-06-25) — [Enable Work IQ](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/enable-work-iq)
- **Local Work IQ CLI/MCP (PUBLIC PREVIEW).** `npm install -g @microsoft/workiq`, then `workiq mcp` (stdio MCP) or `workiq ask -q …`; `--tenant-id` defaults to `common`. "the WorkIQ CLI and MCP Server need to be consented to permissions that require administrative rights on the tenant." Requires Node.js 18+ — [microsoft/work-iq (GitHub)](https://github.com/microsoft/work-iq)

### Inferences
- **The product should not depend on Copilot APIs for ingestion.** Per-user Copilot licensing, preview status of Chat and Search, and the 200 requests per hour Retrieval cap make them unsuitable as a continuous pipeline. They fit as optional capabilities:
  - in Copilot-licensed tenants, call Meeting AI Insights for action items and decisions;
  - call the Retrieval API for "grounded Q&A" over SharePoint without local indexing of sensitive content.
- **Microsoft Search `/search/query`** is the GA, no-Copilot-license way to do cross-workload lookups (e.g. "find the documents referenced in this mail thread"). It also helps discover shared or recent files after `sharedWithMe` is retired.
- **A local app could technically launch `workiq mcp` as a child process** and use it as a tool for its agent. However, it is preview, requires Copilot licensing (per Agent 365) plus admin consent, mutations are blocked by default, and it adds a Node.js dependency. Treat it as an experimental integration only.
- **The Enterprise MCP Server is out of scope** (directory data only).

### Gaps
- No explicit "GA" announcement date for the Retrieval API v1.0 was fetched. The overview lists it without a "(preview)" tag and a v1.0 endpoint exists, but the Terms of Use remain labeled "(preview)".
- Not confirmed whether the Work IQ remote MCP endpoint can be called by arbitrary third-party OAuth clients. The Agent 365 page lists only Microsoft clients; the GitHub CLI works in GitHub Copilot/VS Code. Also unconfirmed: whether the local CLI requires a Copilot license for every call.

---

## 7. Deprecations and timeline (EWS, Outlook REST v2, basic auth / SMTP AUTH, COM/VSTO add-ins and new Outlook, Azure AD Graph, others)

### Takeaway
Build **only on Microsoft Graph (v1.0)**.
- **EWS in Exchange Online:** phased blocking starts **October 1, 2026**; it is **permanently disabled April 1, 2027**, with no exceptions.
- **Outlook REST v2:** decommissioned March 2024.
- **Azure AD Graph:** fully retired August 31, 2025.
- **SMTP AUTH Basic auth:** default-off at the end of December 2026. The earlier March–April 2026 dates are superseded.
- **VSTO/COM add-ins:** don't run in new Outlook for Windows. Use Graph or web add-ins, not COM/MAPI hooks.
- Also note: `sharedWithMe` stops returning data in November 2026, and Event Hubs SAS auth for Graph notifications is deprecated.

### Cited Findings
- **EWS phased disablement (Exchange Team, Feb 5, 2026):** [Exchange Online EWS, Your Time is Almost Up](https://techcommunity.microsoft.com/blog/exchange/exchange-online-ews-your-time-is-almost-up/4492361)
  - Uses the tenant `EWSEnabled` property (True/False/Null). Any tenant still at Null on **October 1, 2026** is switched to False, blocking EWS for all apps.
  - Admins can set True with an AppID Allow List, or revert to Null via PowerShell until the final date. Tenants that set True plus an allow list by the end of September 2026 are excluded from the automatic flip. Microsoft may run temporary "scream tests".
  - "Final EWS shutdown – April 1, 2027 … The ability to control EWSEnabled will be removed". "There will be no exceptions past April 2027."
  - It applies only to Exchange Online; Exchange Server is unaffected.
- **EWSAllowedAppIDs (Exchange Team, June 19, 2026).** A tenant-level AppID allow list, rolling out from June 2026. From October 2026, EWSEnabled=True with an empty list means **all EWS blocked** (except org relationships) — [Introducing EWSAllowedAppIDs](https://techcommunity.microsoft.com/blog/exchange/introducing-ewsallowedappids-preparing-for-the-final-phase-of-ews-retirement/4529471)
- **EWS parity-gap roadmap** (upd. 2026-09-04): [Deprecation of EWS in Exchange Online](https://learn.microsoft.com/en-us/exchange/clients-and-mobile-in-exchange-online/deprecation-of-ews-exchange-online)
  - Q3 CY2026: Notes (IPM.StickyNote), Contact Lists, additional contact properties.
  - Q4 CY2026: archive, public-folder and group import-export; In-Place Archive CRUD; Exchange Admin API; Report Message; non-draft MIME create/update; User Configuration objects; Mark All As Read.
  - Won't be added: generic Public Folder CRUD, generic group-mailbox CRUD, Discovery Mailbox access.
  - Timeline restated: October 2026 disablement starts, April 2027 fully disabled.
- **Conflicting third-party date.** A third-party headline (office365itpros, Feb 2026) referenced "May 2027". The official Microsoft post says April 1, 2027. Treat April 1, 2027 as authoritative — [Exchange Team blog](https://techcommunity.microsoft.com/blog/exchange/exchange-online-ews-your-time-is-almost-up/4492361)
- **Outlook REST API v2.0.** "deprecated … The v2.0 REST endpoint will be fully decommissioned in March 2024" — [Outlook REST API v2.0 (previous versions)](https://learn.microsoft.com/en-us/previous-versions/office/office-365-api/api/version-2.0/use-outlook-rest-api)
- **SMTP AUTH Basic authentication.**
  - The original plan was rejections from March 1, 2026, reaching 100% on April 30, 2026. It was **revised on 1/27/2026**:
    - unchanged until December 2026;
    - **end of December 2026: disabled by default for existing tenants** (admins can re-enable);
    - new tenants created after December 2026: unavailable by default;
    - **second half of 2027: final removal date to be announced**.
  - Sources: [Exchange Team: SMTP AUTH Basic auth retirement](https://techcommunity.microsoft.com/blog/exchange/exchange-online-to-retire-basic-auth-for-client-submission-smtp-auth/4114750); [Updated SMTP AUTH Basic Authentication Deprecation Timeline (Jan 27, 2026)](https://techcommunity.microsoft.com/blog/exchange/updated-exchange-online-smtp-auth-basic-authentication-deprecation-timeline/4489835)
- **Azure AD Graph.** Timeline: August 31, 2024 (new apps need opt-in); February 1, 2025 (all apps need opt-in); **August 31, 2025: "End of extended access … fully retired."** — [Migrate from Azure AD Graph](https://learn.microsoft.com/en-us/graph/migrate-azure-ad-graph-overview)
- **New Outlook for Windows and add-ins.** "VSTO and COM add-ins aren't supported in the new Outlook on Windows … you must migrate your VSTO or COM add-in to an Outlook web add-in". "VSTO and COM add-ins are still supported in classic Outlook on Windows." When users switch, web add-in counterparts replace COM add-ins (upd. 2026-09-25) — [Outlook add-ins in new Outlook](https://learn.microsoft.com/en-us/office/dev/add-ins/outlook/one-outlook)
- **sharedWithMe.** Deprecated; stops returning data after November 2026 — [drive: sharedWithMe](https://learn.microsoft.com/en-us/graph/api/drive-sharedwithme?view=graph-rest-1.0)
- **Event Hubs SAS.** "Authenticating Event Hubs by using shared access signatures (SAS) is deprecated. Use Microsoft Entra ID role-based access control (RBAC) instead." — [Event Hubs delivery](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-event-hubs)

### Inferences
- **No legacy protocols.** Do not use EWS, IMAP/POP/SMTP Basic, Outlook COM/MAPI (Redemption, VSTO) or Outlook REST v2. Graph with delegated OAuth is the only future-proof path, and it works regardless of whether users run new or classic Outlook.
- **Optional Outlook integration.** A "surface drafts inside Outlook" feature should be an **Outlook web add-in** (Office.js), not a COM add-in. Drafts created via Graph already appear natively in the user's Drafts folder in every Outlook client.

### Gaps
- No verified official end-of-support date for classic Outlook for Windows was fetched. The web-search budget was exhausted before this could be checked.

---

## 8. Writing back safely ("human approves before execution"): drafts, reply drafts, tasks, categories/flags, tentative calendar items

### Takeaway
Everything the app writes should be **non-executing artifacts created only after in-app approval**:
- **Mail:** create reply drafts with `createReply`/`createReplyAll` (Mail.ReadWrite). Never call `reply`/`send`, and never request Mail.Send.
- **Tasks:** create To Do tasks (Tasks.ReadWrite, delegated only) or Planner tasks.
- **Mail metadata:** categories/flags via PATCH (Mail.ReadWrite; creating master categories needs MailboxSettings.ReadWrite).
- **Calendar:** POSTing an event **with attendees immediately sends invitations, and this can't be configured**. Proposals must stay in-app until approval, or be created as attendee-less holds (showAs tentative).

### Cited Findings
- **createReply.** "Create a draft to reply to the sender of a message in either JSON or MIME format … You can update the draft later … Send the draft message in a subsequent operation. Alternatively, reply to a message in a single operation." Delegated least privilege is **Mail.ReadWrite**. Endpoint: `POST /me/messages/{id}/createReply` (upd. 2026-06-19) — [message: createReply](https://learn.microsoft.com/en-us/graph/api/message-createreply?view=graph-rest-1.0)
- **Create message.** `POST /me/messages` (or into a folder) "Create a draft of a new message … By default, this operation saves the draft in the Drafts folder." Permission: Mail.ReadWrite — [Create message](https://learn.microsoft.com/en-us/graph/api/user-post-messages?view=graph-rest-1.0)
- **Mail.ReadWrite (delegated)** "Allows the app to create, read, update, and delete email in user mailboxes. Does not include permission to send mail." — [Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
- **Immutable IDs for drafts.** Create the draft with `Prefer: IdType="ImmutableId"` to track it after the user sends it (the Sent Items copy keeps the ID) — [Immutable IDs](https://learn.microsoft.com/en-us/graph/outlook-immutable-id)
- **Events.**
  - "When you create an event that includes attendees, the server sends invitations to all attendees. This ensures consistency … and can't be configured." Create events needs Calendars.ReadWrite (upd. 2026-08-03) — [Create event](https://learn.microsoft.com/en-us/graph/api/user-post-events?view=graph-rest-1.0)
  - `showAs` supports `free`, `tentative`, `busy`, `oof`, `workingElsewhere`, `unknown`.
  - `isDraft` is true only "if the user has updated the meeting in Outlook but hasn't sent the updates to attendees" — [event resource](https://learn.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0)
- **To Do.** `POST /me/todo/lists/{id}/tasks`: delegated Tasks.ReadWrite; **application not supported** — [Create todoTask](https://learn.microsoft.com/en-us/graph/api/todotasklist-post-tasks?view=graph-rest-1.0)
- **Planner.** `POST /planner/tasks` (planId required): delegated Tasks.ReadWrite (higher: Group.ReadWrite.All); application Tasks.ReadWrite.All — [Create plannerTask](https://learn.microsoft.com/en-us/graph/api/planner-post-tasks?view=graph-rest-1.0)
- **Master categories.** `POST /me/outlook/masterCategories` requires **MailboxSettings.ReadWrite** — [Create outlookCategory](https://learn.microsoft.com/en-us/graph/api/outlookuser-post-mastercategories?view=graph-rest-1.0)
- **Microsoft's own mutation default.** Work IQ MCP's tenant policy "blocks mutation operations by default" — [Work IQ MCP tool reference](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/mcp/tool-reference)

### Inferences
- **Approval state machine.** Proposal (local only) → Approved (local) → Materialized (Graph write, storing the immutable ID and the ETag returned) → the user acts in Outlook, To Do or Planner. Record every Graph write in an append-only local audit log with request ID, scope and before/after snapshot.
- **Endpoint allow-list.** Allow-list write endpoints in code: `createReply`, `createReplyAll`, `POST /messages`, `PATCH` categories/flag only, `POST todo tasks`, `POST planner tasks`, `POST events` without attendees. Block `/send`, `/reply`, `/replyAll`, `/forward`, `sendMail` and event creation with attendees unless approved per item. Not requesting Mail.Send makes the send block enforceable at the token level.
- **Calendar proposals.** Create a private attendee-less "hold" with `showAs: tentative` and a marker category. Only after explicit approval create the real meeting with attendees, or open the draft in Outlook for the user.
- **Concurrency.** Write-backs share the Outlook limit of 4 concurrent requests per mailbox. Serialize them and use `If-Match` ETags to avoid overwriting user edits.

### Gaps
- The `createReplyAll` and `createForward` pages, and the message PATCH for `categories`/`flag`, were not individually fetched. They are assumed to mirror createReply (Mail.ReadWrite).
- No Graph-native "draft event" (unsent meeting request) mechanism was found in v1.0.

---

## 9. Sensitivity labels / Purview Information Protection metadata and encrypted (IRM/MIP) content

### Takeaway
- **Files:** Graph can read labels via `POST …/driveItem/extractSensitivityLabels` (v1.0, Files.Read.All). Applying labels (`assignSensitivityLabel`) is a **protected and metered** API with a tenant rate limit.
- **Label catalog and rights:** available through the `security/sensitivityLabel` APIs. Several are **beta**, and SensitivityLabels.Read.All requires admin consent.
- **Encrypted content:** decryption of protected mail (.msg/.rpmsg) and files requires the **MIP SDK (v1.18)**. Copilot APIs honor labels server-side.
- **Recommendation:** read label metadata, respect "hasProtection", and avoid decrypting locally unless the customer explicitly wants it and licenses allow it.

### Cited Findings
- **extractSensitivityLabels** (upd. 2026-07-24): [driveItem: extractSensitivityLabels](https://learn.microsoft.com/en-us/graph/api/driveitem-extractsensitivitylabels?view=graph-rest-1.0)
  - `POST /me/drive/items/{id}/extractSensitivityLabels` (also drives, sites, groups, users).
  - Delegated and application least privilege: **Files.Read.All**.
  - Returns stored label metadata, or re-extracts from the file stream if it is stale. Not supported for SharePoint Embedded.
- **assignSensitivityLabel.** "considered as protected. Protected APIs require you to have more validations, beyond permission and consent" and "This is a metered API and some charges for use may apply." It applies labels to files at rest (upd. 2026-08-19) — [driveItem: assignSensitivityLabel](https://learn.microsoft.com/en-us/graph/api/driveitem-assignsensitivitylabel?view=graph-rest-1.0). The SharePoint tenant limit for "Assign Sensitivity Label" is 100 per 5 min — [SharePoint throttling](https://learn.microsoft.com/en-us/sharepoint/dev/general-development/how-to-avoid-getting-throttled-or-blocked-in-sharepoint-online)
- **Label catalog (BETA).** `microsoft.graph.security.sensitivityLabel` exposes `hasProtection`, `applicableTo` (email, file, site, …), `applicationMode` and `contentFormats`. Methods include List/Get, "Extract content label" (given contentInfo), "Compute rights and inheritance", "Evaluate application/removal/classification result" and "Get usage rights included" (upd. 2025-12-03) — [security sensitivityLabel (beta)](https://learn.microsoft.com/en-us/graph/api/resources/security-sensitivitylabel?view=graph-rest-beta)
- **Label permissions.** SensitivityLabels.Read.All requires admin consent (delegated and app). InformationProtectionPolicy.Read (delegated, "Read user sensitivity labels and label policies") does not — [Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
- **MIP SDK (latest referenced 1.18).** Lets third-party apps "reason over MIP-encrypted information" and apply or remove labels and protection. Supported on Windows and Linux (upd. 2026-09-21) — [MIP SDK overview](https://learn.microsoft.com/en-us/information-protection/develop/overview)
- **MIP SDK and email.** Decrypts and encrypts .msg (Outlook/Exchange) and .rpmsg. For .rpmsg input it "inspects body bytes and attachments" (a DLP-inspection use case); .eml is supported from 1.17 — [MIP SDK email processing](https://learn.microsoft.com/en-us/information-protection/develop/concept-email)
- **Copilot APIs and labels.** They respect sensitivity labels, "If a document, email, or other item has a sensitivity label that restricts access or applies encryption, the API honors those restrictions" — [Copilot APIs security](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/copilot-apis-security-authentication)
- **No label field on messages.** The v1.0/beta `message` resource pages fetched contain no sensitivity-label property (the event resource's `sensitivity` is the legacy Outlook normal/personal/private/confidential flag) — [message resource (beta)](https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-beta); [event resource](https://learn.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0)

### Inferences
- **Mail labels.** For mail, the label is likely only obtainable from message headers or extended properties (e.g. the `msip_labels` header) or by passing content to `extractContentLabel`. Treat it as best-effort and validate empirically.
- **Encrypted mail and files.** Graph returns ciphertext or containers for encrypted items. The app should record "protected, content unavailable" plus label metadata instead of decrypting, unless the customer opts in to MIP SDK integration. Decryption needs its own Entra app/consent for the Azure RMS/Purview protection service, and raises compliance questions for a local AI pipeline.
- **Local policy enforcement.** Build label-aware rules: e.g. exclude "Highly Confidential" or `hasProtection` items from LLM prompts or cloud LLM calls, and display the label on every evidence citation.

### Gaps
- Not verified from a primary source: exactly what Graph returns for IRM/OME-protected messages (body placeholder plus a `message.rpmsg` attachment is the commonly reported behavior). No Graph doc stating this was fetched.
- Not confirmed: whether a v1.0 Graph API exposes the sensitivity label ID of an email message directly.

---

## 10. Official Microsoft Graph SDKs and recommended stack for a .NET Windows service

### Takeaway
Use the **Microsoft Graph .NET SDK v6** (`Microsoft.Graph` 6.7.0, released 2026-09-18; Kiota-based; targets net8.0 and net10.0) with **MSAL.NET 4.90.x** (plus Broker and Extensions.Msal) behind a custom `IAuthenticationProvider` or `TokenCredential`, or raw `HttpClient`. The SDK provides retry/Retry-After handling, paging, batching and delta helpers. Kiota can generate a trimmed client if package size matters. SDKs are community-supported on GitHub; Microsoft CSS supports the HTTP APIs, not the SDKs.

### Cited Findings
- **SDK structure and languages.** Each SDK has a service library (generated models and request builders) and a core library (retry handling, redirects, auth, compression, paging, batching). Languages: C#, CLI, PowerShell, TypeScript/JavaScript, Java, Go, PHP, Python. Don't use preview SDKs in production, and don't use GA SDKs that call the beta endpoint in production. Kiota can generate a smaller client for a subset of APIs. "Microsoft CSS doesn't officially support SDKs but Microsoft supports the HTTP request" — [Graph SDKs overview](https://learn.microsoft.com/en-us/graph/sdks/sdks-overview)
- **Graph .NET SDK v6.0.0 (2026-05-12).** Breaking changes: "update microsoft-graph-core to 4.x"; "Dropped net5.0 … now targets netstandard2.0, netstandard2.1, net8.0, and net10.0" — [msgraph-sdk-dotnet CHANGELOG](https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/main/CHANGELOG.md)
- **Package versions (NuGet, checked 2026-09-27).**
  - `Microsoft.Graph` **6.7.0** (published 2026-09-18). The last 5.x was 5.100.0 on 2026-01-07.
  - `Microsoft.Graph.Beta` 6.7.0-preview.
  - `Azure.Identity` 1.21.0; `Microsoft.Kiota.Abstractions` 2.1.2.
  - MSAL packages 4.90.1 (2026-09-25).
  - Sources: [NuGet Microsoft.Graph](https://www.nuget.org/packages/Microsoft.Graph); [NuGet Microsoft.Identity.Client](https://www.nuget.org/packages/Microsoft.Identity.Client)
- **Stale Learn snippets.** Many Learn API pages still say "Code snippets are only available for the latest version. Current version is 5.x" for C#. Snippets also say Java is 6.x, Python 1.x and Go v1.x — e.g. [driveItem: delta](https://learn.microsoft.com/en-us/graph/api/driveitem-delta?view=graph-rest-1.0); [Event Hubs delivery](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-event-hubs)
- **Auth integration.** The .NET SDK "supports the use of TokenCredential classes in the Azure.Identity library". "The recommended library for authenticating against Microsoft Identity (Azure AD) is MSAL." — [msgraph-sdk-dotnet README](https://github.com/microsoftgraph/msgraph-sdk-dotnet)
- **Built-in retry.** "Microsoft Graph SDKs already implement handlers that rely on the Retry-After header or default to an exponential backoff retry policy." — [Throttling guidance](https://learn.microsoft.com/en-us/graph/throttling)

### Inferences
- **For a .NET 8/10 Windows service.**
  - `Microsoft.Graph` v6 plus a small custom `IAccessTokenProvider` that pulls tokens from the in-session MSAL/WAM broker component (see §1) or from a DPAPI-protected MSAL cache.
  - Keep a thin raw-HTTP path for delta loops, so deltaLink URLs can be replayed verbatim and `Prefer` headers (ImmutableId, maxpagesize, deltashow…) and `User-Agent: ISV|Company|App/Version` are controlled precisely.
  - Register the SDK's RetryHandler and add a per-mailbox concurrency limiter (max 4).
- **Pin versions and test on SDK major upgrades.** The v5→v6 jump happened in May 2026 and docs lag behind.
- **Other SDKs.** The TypeScript, Python, Java and Go SDKs are viable for helper tools, but the service should standardize on .NET for WAM/MSAL broker support on Windows.

### Gaps
- No official maturity or support-level statement per language SDK (GA vs preview for each language, as of 2026) was fetched beyond the overview's general guidance.
