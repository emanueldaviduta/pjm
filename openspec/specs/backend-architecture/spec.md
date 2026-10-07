# backend-architecture Specification

## Purpose

Defines how the backend solution is divided into layers and which layer may depend on which, so that business rules stay independent of HTTP, EF Core providers and other infrastructure, and can be tested on their own.

## Requirements

### Requirement: Four layers with a one-way dependency rule
The backend SHALL be split into four projects named Domain, Application, Infrastructure and Api. Domain SHALL reference no other project or external package. Application SHALL reference only Domain. Infrastructure SHALL reference Application. Api SHALL reference Application and Infrastructure. No other project reference SHALL exist between them.

#### Scenario: Domain has no dependencies
- **WHEN** the Domain project file is inspected
- **THEN** it contains no project references and no package references

#### Scenario: Application does not see infrastructure
- **WHEN** the Application project file is inspected
- **THEN** its only project reference is Domain, and it references no database provider, JWT or ASP.NET Core hosting package

#### Scenario: A reverse dependency is rejected
- **WHEN** a project reference from Domain to Application, or from Application to Infrastructure or Api, is added
- **THEN** the dependency check for the solution fails

### Requirement: Use cases live in services behind interfaces
Business operations SHALL be implemented as services in Application, each exposed through an interface defined in Application. Services SHALL NOT depend on HTTP types, and SHALL report not-found, conflict and invalid-input outcomes as values or results that the Api layer maps to HTTP statuses.

#### Scenario: Service can run without a web host
- **WHEN** a service is constructed in a test with a database context and no ASP.NET Core host
- **THEN** its operations can be called and their outcomes asserted

#### Scenario: Not-found outcome
- **WHEN** a service is asked for an item that does not exist
- **THEN** it returns a not-found outcome and the endpoint answers with HTTP 404

### Requirement: Endpoints only translate HTTP
Each endpoint handler in Api SHALL do no more than read the request, call a service and convert the service's outcome into an HTTP response. Endpoint handlers SHALL NOT query the database, hash passwords or apply business rules.

#### Scenario: Handler delegates to a service
- **WHEN** an endpoint handler is inspected
- **THEN** its body calls one service operation and maps the result, with no data access and no business rules

### Requirement: Persistence behind an Application abstraction
Application SHALL access data only through an abstraction of the database context that it defines. The EF Core context, provider packages, provider selection and migrations SHALL live in Infrastructure.

#### Scenario: Provider choice is not visible to services
- **WHEN** the configured provider changes from SQLite to PostgreSQL
- **THEN** no file in Domain, Application or Api other than configuration is changed

#### Scenario: Migrations are owned by Infrastructure
- **WHEN** a new migration is generated
- **THEN** it is created in the Infrastructure project, and applying it requires no change in Application

### Requirement: Layers are wired through registration extensions
Application and Infrastructure SHALL each expose one registration extension that adds their services to dependency injection, and the Api startup SHALL call both. The Api startup SHALL NOT register individual services or the database context itself.

#### Scenario: Startup uses the extensions
- **WHEN** the Api startup code is inspected
- **THEN** it calls the Application and Infrastructure registration extensions and contains no registration of an individual service or of the database context

#### Scenario: A new service is registered in its own layer
- **WHEN** a new service is added to Application
- **THEN** it is registered inside the Application registration extension, and the Api startup is unchanged
