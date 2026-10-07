# Design

## Context

The frontend is Angular 22 with standalone components, signals, PrimeNG and Tailwind/Less. Data services follow one pattern (see `ProjectService`): a private signal holding the list, a `LoadState` signal (`idle | loading | ready | unauthorized | error`), `load()` that is a no-op once loaded, and mutations that update the signal with `tap`. `ProjectDetail` already composes child components (`TaskDetails`) and uses `MessageService` toasts. The API contract is defined by `add-project-roles` (`GET /api/roles`, `GET/POST/DELETE /api/projects/{projectId}/members...`) and `GET /api/account/users`, which already returns `AccountDto` with `Id`. See proposal.md for motivation and the spec for required behavior.

## Goals / Non-Goals

**Goals:**
- A members section that is understandable at a glance and usable in two or three clicks.
- Reuse existing patterns and PrimeNG components; no new dependency.

**Non-Goals:**
- Role-based hiding of controls, role editing, invitations, a global "people" page.
- Backend changes.

## Decisions

**1. A dedicated `ProjectMembers` component, shown in a Members tab at the top of the project page.** Under the project header, a PrimeNG `Tabs` control has two tabs: Tasks (the existing toolbar, board and list) and Members (the new component). The old "About this project" side panel is removed; the aside now appears only when a task is selected, to show its details. The component is standalone and takes the project id as an input, so `ProjectDetail` only gains the tabs. Alternative: keep the members block in the side panel; rejected by the user in favor of tabs, which give the list more room. Alternative: a separate `/projects/:id/members` route; rejected as extra navigation for a small list.

**2. Add flow in a PrimeNG dialog with a searchable user picker.** Dialog fields: a user autocomplete (filters the in-memory user list by name or email, fine for this app's scale) and a role select defaulting to `Member`. The user list is fetched once through a small `UserDirectoryService` (signal + `LoadState`) and reused. Alternative: server-side search; rejected because the endpoint has no query parameter and the list is small. Users who already hold roles are shown with a "member" tag and the roles they already hold are disabled, which makes the duplicate rule (spec) visible before the server has to answer 409.

**3. Row-level role editing with chips.** Each member row shows role chips with a small remove (x) control, plus a "+" that opens a menu of the roles the member does not yet hold. A "Remove from project" action in the row calls `DELETE .../members/{userId}`. Removing the last role also asks for confirmation, since the member would vanish (matches the spec). Alternative: one edit dialog per member; rejected as heavier than a chip toggle.

**4. Confirmation through PrimeNG `ConfirmDialog`.** Provided at the component level to avoid app-wide providers; used only for full removal.

**5. State and updates: the server is the source of truth.** `ProjectMemberService` keeps members per project in a signal keyed by project id and, after any add or remove, reloads that project's members. It does not apply optimistic changes, because the spec requires the list never to show an unsaved change; a failed call also reloads to the server state. On 401 it sets `unauthorized`, which the component renders with a sign-in link, the same way `ProjectDetail` does.

**6. Role badges use text plus an icon, not color alone.** `Owner` gets a shield icon and `Member` a user icon, each with the role name as text, to satisfy the accessibility requirement.

**7. Models.** `Role { id, name }` and `ProjectMember { userId, firstName, lastName, email, roles: Role[] }` mirror `RoleDto` and `MemberDto`. Initials for avatars come from the names. If `add-project-roles` changes a field name, only these models and the service change.

## Risks / Trade-offs

- [The API contract may evolve] → Only the models and services in the frontend depend on its fields and routes, so a change touches those and nothing else.
- [Any authenticated user sees and uses these controls, since authorization is a later change] → Stated as out of scope; the component is structured so a future `canManage` input can hide actions.
- [Client-side user search will not scale to thousands of users] → Acceptable now; revisit with a server-side search endpoint if the user table grows.
- [Concurrent edits by two people] → Reload after each action and after errors, so the list converges on server state.

## Migration Plan

Frontend only and additive, no feature flag. Rollback is reverting the commit. No data migration.

## Open Questions

- Whether a project's creator is added as `Owner` automatically is deferred to the authorization change (per `add-project-roles`); the UI works the same either way.
