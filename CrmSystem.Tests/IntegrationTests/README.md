# Integration Tests

This directory contains integration tests for the CRM System API.

## Prerequisites

- **Docker**: Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a real PostgreSQL database in a Docker container. Docker must be running before executing these tests.

## Running Integration Tests

1. Ensure Docker is running on your machine
2. Run the tests:

```bash
cd CrmSystem.Tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## Test Coverage

The integration tests cover:

### Customer API Tests (`CustomerApiTests.cs`)
- Create customer with valid/invalid data
- Get customer by ID
- Update customer with concurrency control
- Delete customer (soft delete)
- List customers with filtering, searching, and pagination
- Uniqueness constraint validation
- Response format validation

### Interaction API Tests (`InteractionApiTests.cs`)
- Create interaction for customer
- LastInteractionAt synchronization
- Get interactions ordered by HappenedAt
- Update interaction
- Delete interaction with LastInteractionAt recalculation

### Concurrency Tests (`ConcurrencyTests.cs`)
- Optimistic concurrency control with ETag
- Conflict detection on stale updates
- Conflict detection on stale deletes
- ETag format validation

### Health API Tests (`HealthApiTests.cs`)
- Health endpoint returns healthy status
- Health endpoint does not expose sensitive information

## Test Architecture

- `CrmApiFactory`: Custom `WebApplicationFactory` that uses Testcontainers to create a PostgreSQL container
- `IntegrationTestBase`: Base class providing common test utilities
- `IntegrationTestCollection`: xUnit collection fixture for sharing the database container across tests
