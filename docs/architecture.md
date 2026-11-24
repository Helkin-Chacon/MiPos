
# MiPos - Software Architecture

## 1. Overview

MiPos is a point-of-sale and business management system initially
focused on restaurants and small businesses in Colombia.

The backend follows a modular monolith architecture combined with
a pragmatic Clean Architecture approach and feature-based organization.

The main architectural goals are:

- Maintainability
- Simplicity
- Reliability
- Testability
- Local operation without internet access
- Incremental development
- Future cloud compatibility

## 2. Solution Structure

    MiPos/
    ├── AGENTS.md
    ├── README.md
    ├── docs/
    │   ├── database.dbml
    │   ├── architecture.md
    │   ├── business-rules.md
    │   └── api-conventions.md
    ├── src/
    │   ├── MiPos.Api/
    │   ├── MiPos.Application/
    │   ├── MiPos.Domain/
    │   └── MiPos.Infrastructure/
    └── tests/
        ├── MiPos.UnitTests/
        └── MiPos.IntegrationTests/

## 3. Project Responsibilities

### MiPos.Api

The entry point of the application.

Responsibilities:

- REST API controllers
- HTTP request and response handling
- Authentication and authorization configuration
- Dependency injection configuration
- Exception handling middleware
- OpenAPI and Scalar
- Application startup
- Health checks

Controllers must remain lightweight and delegate application
operations to the Application layer.

### MiPos.Application

Contains application use cases and orchestrates business operations.

Responsibilities:

- Application services
- Use cases
- Request and response models
- Input validation
- Application-level exceptions
- Persistence abstractions
- External service abstractions
- Transaction orchestration

Organize features by business module.

Application services may use IMiPosDbContext directly.

Do not introduce a handler, interface, or validator for every
operation unless it provides meaningful value.

### MiPos.Domain

Contains the core business model.

Responsibilities:

- Domain entities
- Value objects when justified
- Enumerations
- Domain exceptions
- Business rules and invariants

The Domain project must not depend on ASP.NET Core,
Entity Framework Core, PostgreSQL, or external integrations.

### MiPos.Infrastructure

Contains technical implementations required by the application.

Responsibilities:

- Entity Framework Core
- PostgreSQL integration
- DbContext implementation
- Entity configurations
- Database migrations
- Authentication infrastructure
- Fiscal integrations
- Printing infrastructure
- Backup mechanisms
- External services

Infrastructure implements abstractions defined by Application.

## 4. Project Dependencies

Allowed project references:

- MiPos.Api -> MiPos.Application
- MiPos.Api -> MiPos.Infrastructure
- MiPos.Application -> MiPos.Domain
- MiPos.Infrastructure -> MiPos.Application
- MiPos.Infrastructure -> MiPos.Domain

MiPos.Domain must not reference other solution projects.

Circular dependencies are not allowed.

## 5. Business Modules

The application is initially organized into the following modules:

- Organization
- Identity
- Typologies
- Customers
- Catalog
- Orders
- RestaurantTables
- Sales
- Payments
- CashRegisters
- Inventory
- Billing
- Printing
- Reporting

Modules are logical boundaries within the same application.

Do not create independent projects, databases, or services
for each module unless explicitly required.

## 6. Feature Organization

Organize application functionality by business module.

Example:

    MiPos.Application/
    ├── Common/
    │   ├── Interfaces/
    │   └── Exceptions/
    └── Catalog/
        └── Products/
            ├── ProductService.cs
            ├── Models/
            └── Validators/

Use feature-specific folders when their size or complexity
justifies them.

Avoid excessive folder nesting and unnecessary abstractions.

## 7. Persistence

Use PostgreSQL as the relational database engine.

Use Entity Framework Core as the primary ORM.

Initial persistence structure:

    MiPos.Infrastructure/
    └── Persistence/
        ├── MiPosDbContext.cs
        ├── Configurations/
        ├── Migrations/
        └── Seed/

Persistence guidelines:

- Use a single DbContext initially.
- Expose IMiPosDbContext in the Application layer.
- Configure entities using IEntityTypeConfiguration<T>.
- Keep database mappings outside domain entities.
- Use EF Core migrations.
- Preserve database constraints and relationships.
- Use explicit transactions when operations require multiple
  persistence steps to succeed atomically.
- Do not implement a generic repository or custom Unit of Work.

Application may depend on EF Core abstractions as a pragmatic
architectural decision, but must not depend on Infrastructure
or PostgreSQL-specific implementations.

## 8. Database Model

The initial database model is documented in:

    docs/database.dbml

The model provides the reference for:

- Tables
- Columns
- Data types
- Primary keys
- Foreign keys
- Unique constraints
- Indexes
- Entity relationships

Database conventions:

- UUID identifiers
- English naming
- snake_case tables and columns
- Plural table names following the DBML
- decimal for monetary values
- UTC timestamps for stored instants
- Explicit relationships and constraints

The DBML is the initial design reference.

EF Core migrations manage the evolution of the physical database.

Keep the DBML updated when approved schema changes are introduced.

Do not implement all database entities at once.

Introduce entities and migrations incrementally as features
are developed.

## 9. Business Data Isolation

The database model supports businesses and branches.

All business-owned operations must respect the corresponding
business boundaries.

Branch-specific operations must also respect branch ownership.

Never trust business or branch identifiers supplied by a client
without validating the user's access.

The initial deployment uses a local database per installation.

The architecture must not assume that only one business or
one branch can exist permanently.

## 10. Typologies

Typologies provide a centralized catalog for shared classifications.

Typology groups and system codes are controlled by the application.

Initial values must be populated through controlled seed processes.

Validate that referenced typologies belong to the expected group.

Operational states and critical business transitions must remain
controlled by application rules.

Typologies must not replace independent business entities
such as products, categories, payment methods, roles, or permissions.

## 11. Transactional Consistency

Critical operations include:

- Confirming sales
- Registering payments
- Opening and closing cash registers
- Registering cash movements
- Updating inventory
- Cancelling or reversing operations

Use database transactions where necessary to preserve consistency.

Prevent invalid or duplicate business operations through appropriate
application rules, database constraints, and concurrency controls.

External integrations must not unnecessarily extend database
transactions or compromise completed local operations.

## 12. Deployment Architecture

### Initial Local Deployment

MiPos is initially installed at customer premises.

    Customer devices
    (Browsers)
         |
    Local network
         |
    MiPos API
         |
    PostgreSQL

The main computer hosts the backend and database.

Additional devices communicate with the backend through
the local network.

Core operations must remain available without internet access.

Windows is the initial customer deployment target.

The backend must support:

- Native execution
- Windows Service deployment
- Self-contained .NET publishing
- Local PostgreSQL connections
- Automatic startup

Docker is not required.

### Future Cloud Deployment

The same backend should remain compatible with cloud hosting.

Do not implement cloud-specific infrastructure, distributed services,
or bidirectional synchronization during the initial development.

Local backup mechanisms and future cloud deployments must not
require changes to core business rules.

## 13. External Integrations

External integrations are implemented in Infrastructure.

Initial integrations may include:

- Electronic billing through Factus
- Receipt and kitchen printing
- Remote database backups

External providers must remain isolated from domain entities.

Internet-dependent operations must support failure handling
and must not unnecessarily block core POS functionality.

## 14. Configuration

Use standard ASP.NET Core configuration mechanisms:

- appsettings.json
- appsettings.Development.json
- Environment variables
- .NET User Secrets for local development

Do not commit credentials or customer-specific secrets.

Deployment-specific settings must not be hardcoded.

## 15. Testing Architecture

### MiPos.UnitTests

Tests for:

- Domain rules
- Application logic
- Financial calculations
- Business validations

### MiPos.IntegrationTests

Tests for:

- Database operations
- API endpoints
- Database constraints
- Transactions
- Critical business workflows

Integration tests must use an isolated test database.

Tests must never execute against a customer database.

## 16. Architectural Principles

- Prefer simplicity over unnecessary flexibility.
- Use native framework capabilities when appropriate.
- Keep business logic independent of external providers.
- Introduce abstractions when they solve an actual problem.
- Develop incrementally.
- Avoid speculative functionality.
- Keep technical decisions consistent with AGENTS.md.
- Update architecture documentation when significant decisions change.
