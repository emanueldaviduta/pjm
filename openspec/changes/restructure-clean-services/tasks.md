# Tasks

## 1. Baseline before moving anything

- [x] 1.1 Back up `backend/WebAPI/pjm.db` outside the repo and confirm `git status` shows which files are already modified; verify the backup file exists and is non-empty
- [x] 1.2 Write a baseline script (kept outside the repo, for example in the scratchpad) that starts the current API, runs the fixed request set from design decision 8 against a temporary user, removes its test data, and saves each request's status code and the JSON key shape of its body plus the `/openapi/v1.json` path list; verify it runs end to end and the saved baseline file contains every request
- [x] 1.3 Record which existing tests pass today (`dotnet test` on `WebAPI.Tests`); verify the recorded result states that the project fails to build because of `AccountControllerTests`

## 2. Solution and project skeletons

- [x] 2.1 Create `backend/src/ProjectManager.Domain`, `ProjectManager.Application`, `ProjectManager.Infrastructure` and `ProjectManager.Api` (net10.0, nullable and implicit usings as today) and the project references from the spec's dependency rule; verify each project's `.csproj` lists exactly the references the spec allows
- [x] 2.2 Recreate `backend/PJMSolution.slnx` including the four projects; verify `dotnet build backend/PJMSolution.slnx` succeeds on the empty skeletons

## 3. Domain

- [x] 3.1 `git mv` `AppUser`, `Project`, `TaskItem` and the `Status` and `Priority` enums into Domain and change their namespace; verify `dotnet build` of Domain succeeds and its `.csproj` has no project or package references

## 4. Application

- [x] 4.1 Add `IAppDbContext` (three `DbSet`s and `SaveChangesAsync`) and the `Microsoft.EntityFrameworkCore` package reference only; verify Application builds and references no provider, JWT or hosting package
- [x] 4.2 `git mv` the DTOs, `ITokenService` and `ITaskService`/`TaskService` into Application, change `TaskService` to depend on `IAppDbContext`; verify Application builds
- [x] 4.3 Add `IAccountService`/`AccountService` containing the logic now in `AccountEndpoints` (same messages and outcomes, password hashing unchanged), with outcome values instead of `IResult`; verify the build and that each former handler branch (success, invalid credentials, duplicate email, unauthorized, wrong current password) has a matching outcome
- [x] 4.4 Add `IProjectService`/`ProjectService` containing the logic now in `ProjectEndpoints` (list excluding deleted, create, soft delete returning the remaining list, not-found); verify the build
- [x] 4.5 Add `AddApplication()` registering the three services; verify it compiles and registers `ITaskService`, `IAccountService` and `IProjectService`

## 5. Infrastructure

- [x] 5.1 `git mv` `AppDb` and the `Migrations` folder into Infrastructure, make `AppDb` implement `IAppDbContext`, add the provider packages and the JWT library it needs, and change namespaces; verify Infrastructure builds
- [x] 5.2 Rewrite the model snapshot and the migration designers so they use the new entity namespaces (migration ids and class names unchanged); verify with a text search that no `WebAPI.` namespace remains in `Migrations`
- [x] 5.3 `git mv` `TokenService` into Infrastructure and add `AddInfrastructure(configuration, environment)` that registers `AppDb` with the current provider selection (SQLite for Development with `ConnectionLocalDb`, Npgsql otherwise with `ConnectionString`), `IAppDbContext` and `ITokenService`; verify the project builds

## 6. Api

- [x] 6.1 `git mv` `Program.cs`, `Endpoints`, the JWT extension, `appsettings*.json`, `Properties`, `pjm.db`, `global-bundle.pem` and `WebAPI.http` into `ProjectManager.Api`, keep the same `UserSecretsId`, add the Api package references (JwtBearer, OpenAPI, Swashbuckle UI) and change namespaces; verify the project builds
- [x] 6.2 Reduce the endpoint handlers to read request, call the service, map the outcome to HTTP, and have `Program.cs` call only `AddApplication()` and `AddInfrastructure(...)` for service registration; verify with a text search that no endpoint file references `IAppDbContext` or `AppDb`, and `Program.cs` registers no individual service
- [x] 6.3 Verify EF tooling from the new layout: run `dotnet ef migrations has-pending-model-changes` and `dotnet ef migrations list` with `--project` Infrastructure and `--startup-project` Api against a copy of `pjm.db`; verify no pending changes and all migrations listed as applied
- [x] 6.4 Run the Api from its new location in Development once; verify it starts, picks up the user secrets and `pjm.db`, and `/openapi/v1.json` lists the same paths as the baseline

## 7. Tests

- [x] 7.1 `git mv` `WebAPI.Tests` to `backend/tests/ProjectManager.Application.Tests`, reference Application and Infrastructure, update `TokenServiceTests` namespaces; verify the project builds
- [x] 7.2 Replace `AccountControllerTests` with `AccountServiceTests` that run the same scenarios through `IAccountService` outcomes; verify every scenario of the old file still has a test and `dotnet test` passes
- [x] 7.3 Add tests for `ProjectService` (list excludes deleted, create, soft delete, not found) on in-memory SQLite; verify `dotnet test` passes
- [x] 7.4 Add a dependency-rule test that parses the four `.csproj` files and fails when Domain has any reference, Application references anything but Domain (or a provider, JWT or hosting package), Infrastructure references anything but Application, or Api references anything but Application and Infrastructure; verify it passes now and fails when a forbidden reference is added temporarily and removed again

## 8. Final verification and cleanup

- [x] 8.1 Re-run the baseline script from 1.2 against the new Api and compare with the saved baseline; verify the path list, status codes and body key shapes are identical, and remove the test data it created
- [x] 8.2 Run `dotnet build` and `dotnet test` on the whole solution plus `npx tsc --noEmit -p tsconfig.app.json` in `frontend`; verify all pass with no new warnings about missing references
- [x] 8.3 Move `todo-list.md` and `project-task-manager-tasks.md` to `backend/`, delete the now empty `backend/WebAPI` and `backend/WebAPI.Tests`, and update paths mentioned in `README.md` and `backend/aws-backend.md` if any; verify a text search for `backend/WebAPI` and `WebAPI.csproj` finds no stale reference outside `openspec/`

## Workflow follow-up

- Update the `backend/WebAPI/...` paths in `openspec/changes/add-project-roles/tasks.md` after this change lands.
- Archive the change once reviewed.
- Follow-ups kept out of scope: DTOs for projects and accounts, a password hasher, repositories and unit of work if wanted later, CI and deployment pointing at `ProjectManager.Api`.
