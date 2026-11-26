
# MiPos - API Conventions

## 1. General Principles

- Use REST conventions.
- Use ASP.NET Core Controllers.
- Use JSON as the primary data exchange format.
- Use English for endpoints and property names.
- Use appropriate HTTP methods and status codes.
- Use traditional method bodies for controller actions.
- Keep controllers lightweight and delegate business operations to the Application layer.
- Avoid unnecessary response wrappers and abstractions.

## 2. API Versioning

Use URL-based versioning.

Base path:

    /api/v1/

Do not introduce additional API versions until breaking
changes require them.

## 3. Endpoint Naming

Use plural resource names and kebab-case.

Expose only operations that support a documented workflow.

Examples:

    /api/v1/businesses
    /api/v1/branches
    /api/v1/products
    /api/v1/categories
    /api/v1/orders
    /api/v1/restaurant-tables
    /api/v1/sales
    /api/v1/payment-methods
    /api/v1/cash-registers
    /api/v1/cash-register-sessions
    /api/v1/typologies

Use explicit action endpoints for business operations
that cannot be represented naturally as CRUD.

Examples:

    POST /api/v1/orders/{orderId}/cancel
    POST /api/v1/orders/{orderId}/charge
    POST /api/v1/cash-register-sessions/open
    POST /api/v1/cash-register-sessions/{cashRegisterSessionId}/close
    POST /api/v1/fiscal-documents/{fiscalDocumentId}/retry

Avoid generic endpoints such as:

    /api/v1/products/create
    /api/v1/products/get-all
    /api/v1/products/update

### Controller Actions and Service Methods

Name controller actions after the operation and the resource. Include the
criterion when a required identifier scopes the result. For example,
`GetBranchesByBusinessId` lists branches for one business, `GetBranchById`
retrieves a single branch, and `CreateBranch` creates a branch. Use
`GetTypologies` for a collection with an optional `groupId` filter.

Use the same descriptive name in the application service interface and
implementation, with the `Async` suffix for asynchronous methods. For
example, `GetBranchesByBusinessId` calls `GetBranchesByBusinessIdAsync`.

Name identifier parameters for the resource they identify, such as
`businessId`, `branchId`, and `typologyId`. In nested routes, use the
parent resource's identifier when the route is scoped to that parent.
For example, `/api/v1/businesses/{businessId}/branches` uses `businessId`,
and `/api/v1/branches/{branchId}` uses `branchId`.

Keep the identifier name consistent across the route template, controller
action, and service method. For `CreatedAtAction`, reference the detail action
with `nameof` and match its route parameter name in the route values so
the `Location` header resolves to the new resource.

## 4. HTTP Methods

- GET: Retrieve resources.
- POST: Create resources or execute business operations.
- PUT: Replace or update a complete resource.
- PATCH: Partially update a resource when justified.
- DELETE: Remove or deactivate a resource when allowed.

Historical commercial transactions must not be physically
deleted through standard CRUD operations.

## 5. HTTP Status Codes

Use standard HTTP status codes:

- 200 OK: Successful operation with a response.
- 201 Created: Resource successfully created.
- 204 No Content: Successful operation without a response.
- 400 Bad Request: Invalid request or input validation failure.
- 401 Unauthorized: Missing or invalid authentication.
- 403 Forbidden: Insufficient permissions.
- 404 Not Found: Resource not found or not accessible.
- 409 Conflict: Duplicate resource, concurrency conflict,
  or incompatible current business state.
- 422 Unprocessable Content: Valid request that cannot be
  processed due to semantic business validation.
- 500 Internal Server Error: Unexpected server failure.
- 503 Service Unavailable: Temporarily unavailable service.

Do not return HTTP 200 for failed operations.

Use the Location header when returning 201 Created
for a resource with a retrieval endpoint.

## 6. Successful Responses

Return business JSON responses as `ApiResponse<T>` from the API layer.
Set `data` to the response DTO or collection and `error` to null.
Construct successful responses explicitly in controllers.
Use `List<T>` as the default collection type for query results.

Example:

    GET /api/v1/products/{productId}

    {
      "data": {
        "id": "8e6ef72c-b481-4d04-a659-d53126d12920",
        "name": "Executive Lunch",
        "price": 18000,
        "isActive": true
      },
      "error": null
    }

The envelope contains only `data` and `error`. OpenAPI, Scalar
and health use their own infrastructure response formats.

Never expose EF Core entities directly.

## 7. Error Responses

Return the actual HTTP error status. For business errors, set
`data` to null and `error` to an `ApiError`.

Example:

    {
      "data": null,
      "error": {
        "title": "Caja abierta",
        "message": "La caja ya tiene una sesión activa.",
        "traceId": "00-ab123456..."
      }
    }

Error guidelines:

- Use centralized exception handling.
- Use `title` and `message` for a short title and a safe user-facing
  message in Spanish.
- Use the same envelope for automatic HTTP 400 validation responses
  and HTTP errors under `/api` generated without a response body.
- Preserve ASP.NET Core's handling of cancelled requests.
- Include a trace identifier for troubleshooting.
- Never expose stack traces, SQL statements, credentials,
  or sensitive internal information.

The frontend uses the HTTP status for programmatic handling and displays
the title and message to the user.

## 8. Validation

Validation responsibilities:

API:
- Request format.
- Required fields.
- Data types.
- Basic input validation.

Application:
- Resource existence.
- Business ownership.
- User permissions.
- Cross-entity validation.
- Business operation preconditions.

Domain:
- Business invariants.
- Valid state transitions.
- Critical business rules.

Prefer native .NET validation mechanisms initially.

Do not introduce additional validation libraries unless
their use provides meaningful value.

## 9. Pagination

Use pagination for potentially large collections.

Example:

    GET /api/v1/products?page=1&pageSize=20

Response:

    {
      "items": [],
      "page": 1,
      "pageSize": 20,
      "totalCount": 0,
      "totalPages": 0
    }

Default values:

- page: 1
- pageSize: 20
- maximum pageSize: 100

Use deterministic ordering when applying pagination.

Small and bounded catalogs, such as typologies or
payment methods, do not require pagination.

A reusable pagination result model is allowed.

## 10. Filtering and Sorting

Use query parameters for filtering.

Examples:

    GET /api/v1/products?search=burger
    GET /api/v1/products?categoryId={categoryId}&isActive=true
    GET /api/v1/orders?status=OPEN
    GET /api/v1/sales?fromDate=2026-11-01&toDate=2026-11-30

Use explicit supported sorting fields.

Example:

    GET /api/v1/products?sortBy=name&sortDirection=asc

Do not expose unrestricted database queries or arbitrary
entity property filtering.

## 11. JSON Conventions

- Use camelCase for JSON property names.
- Represent UUID identifiers as strings.
- Represent typology `id` and `groupId` as JSON numbers. Use an integer
  `groupId` query parameter to filter the catalog and an integer path
  identifier to retrieve an individual typology.
- Represent monetary amounts as JSON numbers.
- Use consistent enum serialization.
- Use ISO 8601 for dates and timestamps.
- Return UTC timestamps for absolute instants.
- Interpret business date filters using the applicable
  business or branch time zone.

Represent operational enum values using descriptive
string values instead of numeric enum indexes.

Example:

    {
      "status": "OPEN",
      "createdAt": "2026-11-24T15:00:00Z",
      "totalAmount": 25000
    }

Use decimal in C# for monetary calculations.

## 12. Authentication and Authorization

Authentication must support local operation without
internet access.

Authorization must be enforced by the backend.

- Use permission-based authorization.
- Validate business ownership.
- Validate branch access where applicable.
- Do not trust client-supplied user or business identifiers
  without validating the authenticated context.
- Do not expose passwords, password hashes, tokens,
  or authentication secrets.

The specific authentication mechanism, session lifetime,
token strategy, and credential storage implementation
will be defined before implementing the Identity module.

## 13. Concurrency and Transactional Operations

Critical operations must handle concurrent requests safely.

Examples:

- Opening cash register sessions.
- Closing cash register sessions.
- Charging orders.
- Registering payments.
- Updating inventory.
- Creating orders for occupied restaurant tables.

Use database constraints, transactions, and concurrency
controls when appropriate.

Prevent duplicate commercial operations.

Return HTTP 409 when a request conflicts with the
current resource state.

Do not introduce dedicated idempotency tables or
distributed locking infrastructure without approval.

## 14. API Documentation

Use OpenAPI and Scalar.

Let OpenAPI infer business controller response schemas from action return
types. Keep actions free of recurring documentation attributes such as
`Produces` and `ProducesResponseType`. Document endpoint behavior,
permissions, and relevant error statuses when the action signature does
not convey them.

Interactive API documentation must be available in
development environments.

Production exposure must be explicitly configured.

## 15. Cancellation and Asynchronous Operations

Use asynchronous methods for database and network operations.

Propagate CancellationToken where appropriate.

Request cancellation must not leave commercial operations
partially persisted.

Use transactions when multiple database changes must
succeed or fail together.

## 16. Health Checks

Expose a basic application health endpoint.

    GET /health

Health checks must not expose sensitive configuration
or infrastructure information.

Database readiness and external integration checks may
be introduced when required.

External provider failures must not incorrectly indicate
that core local POS operations are unavailable.

## 17. General Restrictions

- Do not expose database entities directly.
- Do not return HTTP 200 for errors.
- Do not duplicate business logic across controllers.
- Do not introduce generic CRUD controllers.
- Do not implement speculative endpoints.
- Do not expose sensitive information.
- Do not introduce unnecessary API abstractions.
- Maintain consistency with existing project conventions.
