
# MiPos - Agent Instructions

## Project Overview

MiPos is a point-of-sale and business management system initially
focused on restaurants and small businesses in Colombia.

The project prioritizes simplicity, maintainability, reliability,
and incremental development without unnecessary complexity.

## Technology Stack

- .NET 10 LTS
- C# 14
- ASP.NET Core Web API
- PostgreSQL
- Entity Framework Core 10
- Npgsql
- OpenAPI and Scalar
- Microsoft.Extensions.Logging (ILogger<T>)
- xUnit
- Git

## Architecture

Follow a pragmatic Clean Architecture within a modular monolith.

The solution is organized into:

- MiPos.Api
- MiPos.Application
- MiPos.Domain
- MiPos.Infrastructure
- MiPos.UnitTests
- MiPos.IntegrationTests

Keep business rules independent of external services and infrastructure.

Follow docs/architecture.md for architecture, feature structure, and folder conventions.
Group domain entities by business module under `MiPos.Domain/Entities`.

## Deployment Strategy

MiPos is initially designed for local deployment at customer premises.

- The backend runs locally on the customer's computer or server.
- PostgreSQL runs locally.
- Users access the application through a web browser.
- Multiple devices can connect through the local network.
- Core business operations must work without internet access.
- Internet-dependent integrations must not block core POS operations.
- The backend must support native deployment without Docker.
- Windows is the initial target environment for customer installations.
- Keep the backend compatible with future cloud deployments.
- Do not implement cloud infrastructure or bidirectional synchronization
  unless explicitly requested.

Docker is optional and must not be a mandatory dependency.

## Project Documentation

Read the relevant documentation before implementing a feature:

- docs/database.dbml: database model and relationships.
- docs/architecture.md: technical decisions and architecture.
- docs/business-rules.md: functional requirements and business rules.
- docs/api-conventions.md: API standards and conventions.
- README.md: project overview and setup instructions.

Do not invent business requirements that are not documented.

If documentation conflicts with the implementation, report the conflict
before making significant changes.

## Coding Standards

- Use English for code, classes, methods, variables, and database names.
- Use PascalCase for C# classes and public members.
- Use camelCase for local variables, parameters, and JSON properties.
- Use snake_case for PostgreSQL tables and columns.
- Use plural names for database tables, following the DBML.
- Enable nullable reference types.
- Use asynchronous operations for I/O.
- Propagate CancellationToken where appropriate.
- Prefer explicit, readable, and maintainable code.
- Add comments only when they provide meaningful context.
- Follow existing project conventions.

## Data Access

- Use Entity Framework Core.
- Use a shared MiPosDbContext initially.
- Expose IMiPosDbContext through the Application layer.
- Use IEntityTypeConfiguration for entity mappings.
- Use AsNoTracking for read-only queries when appropriate.
- Use EF Core migrations for database changes.
- Use native EF Core transactions when necessary.
- Preserve business and branch data isolation.
- Use decimal for monetary amounts, never float or double.
- Store timestamps in UTC.
- Keep database constraints consistent with business rules.
- Treat typology and group IDs as permanent integers. Keep them aligned with frontend enums and never reuse them.
- Validate typology references against the expected group.

The DBML is the initial database reference.

Do not introduce schema changes without explaining their purpose.

## API Standards

- Use ASP.NET Core Controllers and follow the controller conventions in docs/api-conventions.md.
- Follow REST conventions.
- Use /api/v1/ for API versioning.
- Use appropriate HTTP status codes.
- Use ApiResponse<T> for business JSON responses and ApiError for error details.
- Implement centralized exception handling.
- Validate incoming requests.
- Never expose sensitive internal information in API responses.

## Logging

- Use the native .NET logging abstraction, ILogger<T>.
- Prefer structured logging over string interpolation.
- Use appropriate log levels.
- Log relevant errors and operational events.
- Avoid excessive or redundant logging.
- Never log sensitive information.
- Keep logging providers replaceable without modifying business logic.

Do not introduce additional logging providers unless justified.

## Architectural Restrictions

Do not introduce the following without explicit approval:

- Microservices
- Generic repositories
- Custom Unit of Work
- MediatR
- AutoMapper
- Full CQRS infrastructure
- Event sourcing
- Redis
- Message brokers
- Mandatory containerization
- Unnecessary abstraction layers
- Additional database tables without a functional justification

Prefer native .NET capabilities before adding external dependencies.

Do not overengineer solutions or implement speculative requirements.

## Security and Reliability

- Never hardcode credentials, tokens, or secrets.
- Enforce authentication and authorization where required.
- Validate business ownership of protected resources.
- Protect sensitive customer and fiscal information.
- Never log passwords, tokens, or payment credentials.
- Preserve transactional consistency in sales, payments,
  cash register operations, and inventory.
- Do not make core POS operations depend on internet availability.

## Testing

- Use xUnit.
- Write unit tests for relevant business rules.
- Write integration tests for critical database and API operations.
- Run relevant tests after implementing changes.
- Build the solution before completing a task.
- Report tests that could not be executed.

## Agent Workflow

Before implementing:

1. Understand the requested feature.
2. Read the relevant project documentation.
3. Inspect existing code and conventions.
4. Identify the simplest appropriate solution.

During implementation:

1. Keep changes focused on the requested task.
2. Avoid unrelated refactoring.
3. Reuse existing components when appropriate.
4. Do not silently modify business rules.
5. Request approval for significant architectural changes
   or destructive operations.

After implementation:

1. Build the solution.
2. Run relevant tests.
3. Summarize the implemented changes.
4. Report technical decisions, limitations, and pending issues.
5. Update documentation when approved decisions change.

## Communication

- Communicate with the development team in Spanish.
- Keep source code and technical identifiers in English.
- Be concise and technically precise.
- Explain significant decisions and trade-offs.
- Do not claim successful execution or validation unless verified.

## Development Philosophy

Build what is needed today.

Keep the architecture extensible, but do not implement functionality
solely because it might be useful in the future.

Favor simplicity without sacrificing correctness, security,
or maintainability.
