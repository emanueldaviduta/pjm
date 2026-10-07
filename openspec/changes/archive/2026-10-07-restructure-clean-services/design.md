# Design

## Context

Today everything is in `backend/WebAPI` (namespace `WebAPI.*`): entities (`AppUser`, `Project`, `TaskItem`, enums), DTOs, `AppDb`, migrations, `TokenService`, JWT setup, endpoints and `Program.cs`. `TaskService` is the only real service; `AccountEndpoints` (password hashing, login, profile) and `ProjectEndpoints` (queries, soft delete) still hold their logic inline. See proposal.md for the motivation and `specs/backend-architecture/spec.md` for the required rules.

Observed facts that shape the plan:
- The dev database is `pjm.db` (SQLite, relative path, committed); production uses Npgsql with a connection string from configuration. The provider is chosen in `Program.cs` by environment.
- The EF model snapshot and migration designers refer to entities by CLR name (`WebAPI.Models.AppUser`, ...). Changing entity namespaces without touching them would make EF see a different model.
- User secrets are keyed by `UserSecretsId` in the csproj (secrets are not in the repo).
- `backend/WebAPI.Tests` (xUnit) is committed but does not compile: `AccountControllerTests` targets the removed `AccountController`. `TokenServiceTests` still fits.
- `PJMSolution.slnx` is deleted in the working tree, so there is currently no solution file.

## Goals / Non-Goals

**Goals:**
- Layout and dependencies as in the spec, with the HTTP contract and database schema unchanged.
- Every layer is testable on its own; the broken test project builds and passes again.
- History preserved (`git mv`), and a repeatable way to prove "nothing changed".

**Non-Goals:**
- Repositories, unit of work, MediatR, FluentValidation, new DTOs for accounts and projects, a new password hasher, role or ownership behavior, CI or deployment changes.

## Decisions

**1. Layout and names.** Solution at `backend/PJMSolution.slnx`:

```
backend/
  PJMSolution.slnx
  src/
    ProjectManager.Domain/           entities, enums (no references)
    ProjectManager.Application/      services, interfaces, DTOs, IAppDbContext, AddApplication()
    ProjectManager.Infrastructure/   AppDb, Migrations, TokenService, AddInfrastructure()
    ProjectManager.Api/              Program.cs, Endpoints, JWT setup, appsettings, pjm.db, pem
  tests/
    ProjectManager.Application.Tests/
```

Names follow `project-task-manager-tasks.md`. One test project references Application and Infrastructure for now (`AppDb` on in-memory SQLite and `TokenService` are needed); Domain and API integration test projects from the plan come later. Alternative: keep the name `WebAPI` for the Api project to shrink the diff; rejected because the namespace change is already happening and mixed naming is confusing.

**2. "Clean Services": no repositories.** Application defines `IAppDbContext` exposing the three `DbSet`s and `SaveChangesAsync`; `AppDb` implements it. Services query it directly, including projections (`Select` straight to DTOs), which a repository would hide. Trade-off: Application references the `Microsoft.EntityFrameworkCore` package (for `DbSet<T>` and async LINQ) but no provider package. This is a deliberate, narrow leak and the spec's "no database provider" wording matches it. Alternative: repositories and unit of work as in the plan, so Application has no EF reference at all; chosen against by the user for now and can be added later behind the same service interfaces.

**3. Account and project logic move into services, unchanged.** `IAccountService` (register, login, list users, get and update the current user, change password) and `IProjectService` (list, create, soft delete) get the code that is in the handlers today, with the same messages, statuses and response shapes. Results are small outcome values (success with data, `NotFound`, `Conflict`, `Invalid`/`Unauthorized` with the existing message), mapped to HTTP in the endpoints, matching how `TaskService` returns `null` for not found. `IProjectService` still returns the `Project` entity and the endpoint still binds `Project` from the body; project and account DTOs are a later change. Alternative: leave those handlers inline and give Api an `IAppDbContext`; rejected because it breaks the "endpoints only translate HTTP" rule and leaves the account logic untestable without a host.

**4. Where JWT and token code goes.** `ITokenService` moves to Application; `TokenService` (needs the JWT libraries) to Infrastructure. The `AddJwtBearer` validation setup stays in Api because it configures ASP.NET Core authentication. Both read the same `TokenKey` configuration value as today.

**5. Registration.** `AddApplication()` registers the three services. `AddInfrastructure(configuration, environment)` registers `AppDb` with the provider choice that is in `Program.cs` today (SQLite in Development using `ConnectionLocalDb`, Npgsql otherwise using `ConnectionString`), maps `IAppDbContext` to the same scoped instance, and registers `TokenService`. `Program.cs` keeps only host concerns (OpenAPI, CORS, authentication, endpoint mapping, applying migrations at startup as it does now).

**6. EF Core migration ownership.** `AppDb` and the migrations live in Infrastructure, so tooling is run as `dotnet ef ... --project src/ProjectManager.Infrastructure --startup-project src/ProjectManager.Api`. The model snapshot and migration designers are rewritten by replacing the old namespaces with the new ones; migration ids and class names are untouched, so `__EFMigrationsHistory` still matches. Verification: `has-pending-model-changes` must report none and `migrations list` must show all migrations as applied against a copy of `pjm.db`. Alternative: keep entities in the old `WebAPI.Models` namespace to avoid touching the snapshot; rejected because it would leave a wrong namespace in Domain permanently.

**7. Files that move with the Api project.** `appsettings*.json`, `Properties/launchSettings.json`, `pjm.db` (so local data is kept), `global-bundle.pem`, `WebAPI.http`, and the `UserSecretsId` (same value, so existing user secrets keep working). The two planning documents in `backend/WebAPI` move to `backend/`.

**8. Proving "no behavior change".** Before moving anything, record a baseline: the paths in `/openapi/v1.json` and the status code and body shape of a fixed set of requests (register, login, me, update me, change password, users, projects list, create and delete, tasks list, create, update, get). Repeat the same requests after the restructuring and compare. Add a test that reads the four project files and fails if the dependency rule is broken (the spec's reverse-dependency scenario).

**9. Tests.** `AccountControllerTests` becomes `AccountServiceTests`, with the same assertions applied to `IAccountService` outcomes instead of `IActionResult`. `TokenServiceTests` only changes namespaces. Existing coverage is kept, not reduced.

## Risks / Trade-offs

- [The snapshot rewrite is wrong and EF generates a rename or drop migration on the next `migrations add`] -> Verify with `has-pending-model-changes` and against a copy of the dev database before finishing; never apply an auto-generated migration produced by this change.
- [Moving `pjm.db` and secrets breaks local runs] -> `git mv` the database, keep the `UserSecretsId`, and run the app once from the new project as a task.
- [Large rename diff hides accidental behavior changes] -> Baseline comparison (decision 8) and moving files before editing them, so the diff of each file stays small.
- [Application references EF Core] -> Accepted trade-off (decision 2); the dependency test only forbids provider, JWT and hosting packages.
- [Account and project services still expose entity-shaped or ad hoc responses] -> Kept on purpose to avoid mixing a restructuring with contract changes; tracked for a follow-up.
- [`add-project-roles` has task paths under `backend/WebAPI`] -> Land this change first, then update the paths in that change's `tasks.md`.

## Migration Plan

1. Record the baseline (decision 8) and make sure `pjm.db` is backed up or recoverable from Git.
2. Create the solution and the four projects, then move files layer by layer with `git mv`, building after each layer.
3. Rewrite the snapshot and designers, verify no pending model changes against a copy of `pjm.db`.
4. Repair and move the tests, add the dependency test, run everything, then re-run the baseline and compare.
5. Remove the empty `backend/WebAPI` and `backend/WebAPI.Tests` folders.
6. Rollback: the work is a pure file move and code relocation with no schema change, so reverting the commit restores the previous layout; no database step is needed.
