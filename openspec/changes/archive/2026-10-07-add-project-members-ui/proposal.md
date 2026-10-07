# Proposal

## Why

`add-project-roles` gives the backend project membership (users in a project, each with one or more roles), but nothing in the Angular app lets a person use it. Without a UI, members can only be managed with raw HTTP calls. This change adds an intuitive way to see who is on a project and to add or remove people from the project page.

## What Changes

- Add a **Members** section to the project detail page: avatars/names of current members with their role badges, and a clear empty state.
- Add an **Add member** flow: search the user list by name or email, pick a person, pick a role (default `Member`), confirm. Users already on the project are marked and cannot be added with the same role twice.
- Let a member's roles be changed in place: add another role, remove one role, or remove the member altogether (with a confirmation for the last role / full removal).
- Show clear feedback: loading, success and error toasts, and a message when the session expired.
- Add frontend models and services for roles and project members that call the `add-project-roles` endpoints.

Not in scope: hiding or disabling these controls based on the signed-in user's role (authorization is a later change), creating or editing roles, inviting people who have no account, notifications, and any backend change.

## Capabilities

### New Capabilities
- `project-members-ui`: what a person sees and can do in the browser to view, add, re-role and remove the members of a project.

### Modified Capabilities

None. The `openspec/specs/` directory holds no specs yet; the API behavior is defined by `add-project-roles`.

## Impact

- **Frontend (`frontend/src/app`)**: new `_models` (role, member), new `_services` (role service, project-member service), a members component used by `project-detail`, small edits to `project-detail.ts/.html/.less`. Uses PrimeNG components already in the app (dialog, select, table or tag, toast, confirm dialog).
- **Backend**: no change. It uses the endpoints from `add-project-roles`, which are already present in `backend/src`.
- **Assumption**: the user picker uses the existing `GET /api/account/users` list. It already returns id, names and email (`AccountDto`), so no backend change is needed for it.
