# Project & Task Manager: Clean Architecture (.NET 10)

Proiect de exercițiu: API backend pentru gestionarea proiectelor și a task-urilor.
Stack: .NET 10, ASP.NET Core, EF Core, PostgreSQL (Npgsql), MediatR, FluentValidation, xUnit.

## Cerințe funcționale (scope basic)

- Utilizatorii se înregistrează și se autentifică.
- Un utilizator creează proiecte și este proprietarul lor.
- Un proiect conține task-uri.
- Un task are titlu, descriere, status (`Todo`, `InProgress`, `Done`), prioritate, termen limită și un utilizator asignat.
- Se pot lista, filtra, actualiza și șterge proiecte și task-uri.

## Structura soluției

```
src/
  ProjectManager.Domain/          # entități, value objects, reguli de business (fără dependențe)
  ProjectManager.Application/     # use cases, CQRS, interfețe, DTO-uri, validări
  ProjectManager.Infrastructure/  # EF Core, repository-uri, JWT, servicii externe
  ProjectManager.Api/             # controllere/endpoints, middleware, DI, configurare
tests/
  ProjectManager.Domain.Tests/
  ProjectManager.Application.Tests/
  ProjectManager.Api.IntegrationTests/
```

Regula de dependențe: `Api → Application → Domain` și `Infrastructure → Application → Domain`. **Domain nu depinde de nimic.**

---

## Faza 0: Setup

- [ ] Creează repo Git și `.gitignore` pentru .NET
- [ ] Creează soluția și cele 4 proiecte (`Domain`, `Application`, `Infrastructure`, `Api`)
- [ ] Setează referințele dintre proiecte conform regulii de dependențe
- [ ] Pune `net10.0` și setările comune în `Directory.Build.props` (nullable, implicit usings, warnings as errors)
- [ ] Adaugă `.editorconfig`
- [ ] Creează proiectele de teste (xUnit)
- [ ] Rulează PostgreSQL local (Docker Compose)

## Faza 1: Domain

- [ ] Clasa de bază `Entity` (Id) și, opțional, `AuditableEntity` (CreatedAt, UpdatedAt)
- [ ] Entitatea `User`
- [ ] Entitatea `Project` (Name, Description, OwnerId)
- [ ] Entitatea `TaskItem` (Title, Description, Status, Priority, DueDate, ProjectId, AssigneeId)
- [ ] Enum-uri: `TaskStatus`, `TaskPriority`
- [ ] Mută regulile în entități: metode precum `Task.Complete()`, `Task.AssignTo(userId)`, `Task.ChangePriority()`
- [ ] Reguli: un task `Done` nu poate fi reasignat; termenul limită nu poate fi în trecut la creare
- [ ] Excepții de domeniu (`DomainException`)
- [ ] Teste unitare pentru regulile din domeniu

## Faza 2: Application

- [ ] Instalează MediatR și FluentValidation
- [ ] Definește interfețele în Application: `IProjectRepository`, `ITaskRepository`, `IUserRepository`, `IUnitOfWork`, `ICurrentUserService`, `IJwtTokenGenerator`, `IPasswordHasher`
- [ ] Creează DTO-uri / modele de răspuns (nu expune entitățile direct)
- [ ] **Commands:** `CreateProject`, `UpdateProject`, `DeleteProject`
- [ ] **Commands:** `CreateTask`, `UpdateTask`, `ChangeTaskStatus`, `AssignTask`, `DeleteTask`
- [ ] **Commands:** `RegisterUser`, `LoginUser`
- [ ] **Queries:** `GetProjects` (cu paginare), `GetProjectById`
- [ ] **Queries:** `GetTasksByProject` (filtre pe status, prioritate, asignat, sortare, paginare)
- [ ] **Queries:** `GetMyTasks`
- [ ] Validatori FluentValidation pentru fiecare command
- [ ] Pipeline behavior MediatR pentru validare automată
- [ ] Pipeline behavior pentru logging (opțional)
- [ ] Excepții de aplicație: `NotFoundException`, `ValidationException`, `ForbiddenException`
- [ ] Autorizare la nivel de use case: doar proprietarul modifică/șterge proiectul
- [ ] Extensie `AddApplication()` pentru înregistrarea serviciilor în DI
- [ ] Teste unitare pentru handlere (repository-uri mock-uite)

## Faza 3: Infrastructure

- [ ] Instalează `Npgsql.EntityFrameworkCore.PostgreSQL` și `Microsoft.EntityFrameworkCore.Design`
- [ ] `AppDbContext` cu `DbSet`-uri
- [ ] Configurări EF cu `IEntityTypeConfiguration<T>` (tabele, lungimi, indexuri, relații)
- [ ] Convenții: nume de tabele și coloane în `snake_case`
- [ ] Implementează repository-urile și `UnitOfWork`
- [ ] Completarea automată a `CreatedAt` / `UpdatedAt` (override pe `SaveChanges` sau interceptor)
- [ ] Prima migrare: `dotnet ef migrations add Initial`
- [ ] Seed de date pentru dezvoltare
- [ ] Implementează `JwtTokenGenerator` și `PasswordHasher`
- [ ] Extensie `AddInfrastructure(configuration)` pentru DI
- [ ] Teste de integrare pentru repository-uri cu Testcontainers (Postgres real)

## Faza 4: Api

- [ ] Configurează `Program.cs` cu `AddApplication()` și `AddInfrastructure()`
- [ ] Controllere sau Minimal APIs: `AuthController`, `ProjectsController`, `TasksController`
- [ ] Controllerele doar trimit comenzi/query-uri prin `IMediator`, fără logică de business
- [ ] Autentificare JWT Bearer și `[Authorize]` pe endpoint-uri
- [ ] Implementează `ICurrentUserService` (citește user-ul din claims)
- [ ] Middleware global pentru erori, care mapează excepțiile pe `ProblemDetails`: 400, 401, 403, 404
- [ ] Documentație OpenAPI (Swagger sau Scalar) cu suport pentru Bearer
- [ ] Configurare CORS pentru UI
- [ ] Versionare API (`/api/v1/...`)
- [ ] Health check endpoint (`/health`) cu verificare Postgres
- [ ] Configurare: User Secrets local, variabile de mediu în producție (nu secrete în cod)

## Faza 5: Teste

- [ ] Teste unitare Domain (reguli de business)
- [ ] Teste unitare Application (handlere, validatori)
- [ ] Teste de integrare API cu `WebApplicationFactory` și Testcontainers
- [ ] Scenarii: înregistrare, login, creare proiect, creare task, schimbare status, accesare fără token, accesare proiect străin
- [ ] Măsoară code coverage (Coverlet)

## Faza 6: Calitate și observabilitate

- [ ] Logging structurat cu Serilog
- [ ] Correlation ID pe request-uri
- [ ] Rate limiting pe endpoint-urile de autentificare
- [ ] Refresh token (opțional)
- [ ] Caching pentru query-urile frecvente (opțional)

## Faza 7: Deploy

- [ ] Dockerfile multi-stage pentru API
- [ ] `docker-compose.yml` cu API + Postgres pentru rulare locală
- [ ] GitHub Actions: build + test la fiecare push
- [ ] GitHub Actions: deploy pe AWS (Elastic Beanstalk) cu OIDC
- [ ] Bază de date în RDS (`db.t4g.micro`, Single-AZ, fără acces public)
- [ ] Rulează migrările controlat la deploy (nu automat la pornirea aplicației în producție)
- [ ] Secretele în SSM Parameter Store sau variabile de mediu

---

## Extra (după ce termini baza)

- [ ] Comentarii pe task-uri
- [ ] Etichete (tags) și căutare full-text
- [ ] Membri într-un proiect, cu roluri (`Owner`, `Member`)
- [ ] Domain events (ex. `TaskCompleted`) cu notificări
- [ ] Soft delete
- [ ] Istoric de modificări (audit log)
- [ ] Refactorizare în Vertical Slice pentru comparație

## Criterii de "gata"

- Dependențele respectă regula: Domain nu referențiază niciun alt proiect.
- Controllerele nu conțin logică de business.
- Toate regulile de domeniu au teste unitare.
- API-ul rulează local cu un singur `docker compose up`.
- Pipeline-ul CI trece verde (build + teste).
