# Proposal

## Why

The backend is a single `WebAPI` project where entities, DTOs, EF Core, JWT, endpoints and business logic live side by side. Only task logic has been moved into a service; account and project logic still sits inside the endpoint handlers. The plan in `project-task-manager-tasks.md` asks for a layered solution (Domain, Application, Infrastructure, Api) with a one-way dependency rule, and upcoming work such as roles, ownership and authorization needs a place for business rules that does not depend on HTTP or EF providers. The test project `WebAPI.Tests` is also broken today: it tests `AccountController`, which no longer exists since the move to Minimal API endpoints. Doing the restructuring now, while the codebase is small, avoids moving more code later.

## What Changes

- Split `backend/WebAPI` into four projects under `backend/src/`: `ProjectManager.Domain`, `ProjectManager.Application`, `ProjectManager.Infrastructure`, `ProjectManager.Api`, with the dependency rule `Api -> Application -> Domain` and `Infrastructure -> Application -> Domain`.
- Use a "Clean Services" style: use cases are services (interfaces and implementations) in Application. There is no MediatR and no repository or unit-of-work layer; services use a thin `IAppDbContext` abstraction, implemented by the EF Core context in Infrastructure.
- Move existing code to its layer: entities and enums to Domain; services, service interfaces, DTOs and `IAppDbContext` to Application; the EF Core context, migrations, provider selection and the JWT token generator to Infrastructure; endpoints, authentication setup, `Program.cs` and configuration to Api.
- Lift the logic currently inside the account and project endpoint handlers into `IAccountService` and `IProjectService` unchanged, so every layer has a testable unit. Request and response shapes are not redesigned in this change.
- Add `AddApplication()` and `AddInfrastructure(...)` registration extensions used by `Program.cs`.
- Recreate the solution file (it is currently deleted from the working tree), move and repair the test project into `backend/tests/`, and keep the dev SQLite database, user secrets and `global-bundle.pem` working from the Api project.
- Rewrite the EF Core model snapshot so the new namespaces do not look like model changes; no new migration is generated and no schema changes.

**No behavior change:** routes, status codes, response bodies, authentication rules and the database schema stay exactly as they are today.

## Capabilities

### New Capabilities
- `backend-architecture`: the layering and dependency rules of the backend solution, where use cases and persistence code live, and how the layers are wired together.

### Modified Capabilities

None. The project has no existing specs under `openspec/specs/`.

## Impact

- **Backend layout**: `backend/WebAPI` is replaced by `backend/src/ProjectManager.*` and `backend/tests/`; code moves with `git mv` so history is kept. Namespaces change from `WebAPI.*` to `ProjectManager.*`.
- **EF Core**: migrations move to Infrastructure; tooling commands need `--project` (Infrastructure) and `--startup-project` (Api). The `__EFMigrationsHistory` ids do not change, so existing databases are unaffected.
- **Run and tooling**: the project to run, the launch settings, the user secrets id and the committed `pjm.db` are now under the Api project. Deployment or CI that points at `WebAPI` (the staging workflow is still a placeholder) must point at `ProjectManager.Api` when it is written.
- **Frontend**: unchanged, since the HTTP contract is preserved.
- **Other in-flight change**: `add-project-roles` has task paths under `backend/WebAPI/...`; those paths must be updated once this change lands.
- **Out of scope**: repositories and unit of work, MediatR, FluentValidation, DTOs for projects and accounts, password hashing replacement, and any role or ownership behavior.
