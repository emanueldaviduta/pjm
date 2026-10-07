# Tasks

## 1. Prerequisites and models

- [x] 1.1 Confirm the `add-project-roles` endpoints exist in `backend/src` (`GET /api/roles`, the `/api/projects/{id}/members` routes), or record that the UI is built against the documented contract; verify by listing them in `/openapi/v1.json` — Confirmed in `backend/src`: `RoleEndpoints.cs` and `ProjectMembershipEndpoints.cs` expose the contract routes, and `MemberDto`, `RoleDto` and `AddMemberRequest` match the frontend models.
- [x] 1.2 Add `Role` and `ProjectMember` models under `frontend/src/app/_models/`; verify `npx tsc --noEmit -p tsconfig.app.json` passes

## 2. Services

- [x] 2.1 Add `RoleService` (loads `GET /roles` once, `LoadState` signal) and `UserDirectoryService` (loads `GET /account/users` once); verify with unit tests using `HttpTestingController` that each loads once and exposes the data
- [x] 2.2 Add `ProjectMemberService` with per-project members signal, `load(projectId)`, `addRole`, `removeRole`, `removeMember`, reload after errors, and `unauthorized` on 401; verify unit tests cover add, duplicate (409), remove, 404 and 401

## 3. Members list

- [x] 3.1 Create the `ProjectMembers` component showing members with initials avatar, name, email and role chips (icon plus text), with loading, empty and error-with-retry states; verify a component test renders each state
- [x] 3.2 Replace the "About this project" side panel of `ProjectDetail` with a Tasks / Members tab control at the top (Members tab holds `ProjectMembers` with the project id; the aside remains only for task details); verify in the running app that both tabs switch, the board and list still work under Tasks, and no "About this project" panel remains

## 4. Add, edit and remove

- [x] 4.1 Implement the add-member dialog: user autocomplete by name or email, role select defaulting to `Member`, confirm disabled until a user is chosen, "no user found" message, existing members marked and their roles disabled; verify component tests for search, empty result, default role and disabled confirm
- [x] 4.2 Wire confirm to `ProjectMemberService` with success and error toasts and a conflict message on 409; verify tests for success, 409 and generic failure
- [x] 4.3 Implement row-level role editing (add-role menu, remove-role chip) and "Remove from project" with `ConfirmDialog` for full removal or removing the last role; verify tests for add role, remove one role, confirm and cancel
- [x] 4.4 Render the unauthorized state with a sign-in link carrying `returnUrl`; verify a test for 401

## 5. Accessibility and integration

- [x] 5.1 Label all controls, manage dialog focus and make every action keyboard reachable; verify by completing add, add-role and remove with the keyboard only in the running app
- [x] 5.2 Run the app against the API with `add-project-roles` applied and walk through every scenario in `specs/project-members-ui/spec.md`; verify each behaves as written, then run `ng test` and `ng build` with no errors

## Workflow follow-up

- Archive this change after `add-project-roles` is archived.
- Role-based visibility of these controls belongs to the future authorization change.
