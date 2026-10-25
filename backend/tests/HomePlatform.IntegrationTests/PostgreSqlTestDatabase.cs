using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests;

// The optional connection must point to a dedicated test server whose user can
// create databases. Every fixture gets a new database; the supplied DB is never migrated.
internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly string? _server = Environment.GetEnvironmentVariable(
        "HOMEPLATFORM_TEST_POSTGRES");
    private PostgreSqlContainer? _container;
    private string? _connectionString;
    private string? _databaseName;

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_server))
        {
            _container = new PostgreSqlBuilder("postgres:18.6-alpine")
                .WithDatabase("homeplatform")
                .WithUsername("homeplatform")
                .WithPassword("homeplatform-dev")
                .Build();
            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
            return;
        }

        _databaseName = $"homeplatform_test_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(_server);
        await connection.OpenAsync();
        // The identifier is generated here, never taken from external input.
        await using var command = new NpgsqlCommand(
            $"CREATE DATABASE \"{_databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
        _connectionString = new NpgsqlConnectionStringBuilder(_server)
        {
            Database = _databaseName,
            Pooling = false
        }.ConnectionString;
    }

    public string GetConnectionString() => _connectionString
        ?? throw new InvalidOperationException("Test database has not started.");

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
        if (_databaseName is null)
            return;

        await using var connection = new NpgsqlConnection(_server);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
