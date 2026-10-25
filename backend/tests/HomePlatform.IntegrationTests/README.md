# PostgreSQL integration tests

By default, `PostgreSqlTestDatabase` starts the existing PostgreSQL 18.6 Testcontainers image. Docker must be running:

```sh
dotnet test backend/HomePlatform.slnx
```

When Docker is unavailable, use a **dedicated disposable PostgreSQL server** and a user with permission to create databases:

```sh
HOMEPLATFORM_TEST_POSTGRES='Host=127.0.0.1;Port=55439;Database=postgres;Username=homeplatform;Pooling=false' \
  dotnet test backend/HomePlatform.slnx -- xUnit.ParallelizeTestCollections=false
```

The supplied database is used only for administrative CREATE/DROP commands. Each test fixture creates a uniquely named `homeplatform_test_<guid>` database, applies its own schema/migrations there, and drops that database at disposal. Existing developer databases must never be used as test fixtures. The helper does not initialize or stop the supplied server.

Serial test collections reduce file/connection pressure on a shared test server. Concurrency regression tests still use distinct requests and DbContext instances with explicit synchronization; they do not depend on collection parallelism or arbitrary delays.

`HouseholdConcurrencyTests` stops the stale HTTP request immediately before its SaveChanges call, commits a competing request in another scope, then releases the stale request and compares the complete relevant database state. Direct persistence tests additionally inspect SQL parameters, equal-timestamp behavior, cancellation, and compatible membership changes. `OwnershipVersionMigrationTests` verifies the fresh chain and an upgrade containing existing data, including conflict detection on initially empty ownership tokens.
