using Npgsql;

namespace DocumentExport.Api.Persistence;

/// <summary>Builds the pooled data source of the exports database.</summary>
public static class ExportsDataSourceFactory
{
    public const string ConnectionStringName = "Exports";
    public const string PasswordVariable = "EXPORTS_DB_PASSWORD";

    /// <summary>
    /// Uses the "Exports" connection string; when <c>EXPORTS_DB_PASSWORD</c> is set (staging and production), it
    /// supplies the password instead of the connection string.
    /// </summary>
    public static NpgsqlDataSource Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var password = Environment.GetEnvironmentVariable(PasswordVariable);
        if (!string.IsNullOrEmpty(password))
        {
            builder.Password = password;
        }

        return new NpgsqlDataSourceBuilder(builder.ConnectionString).Build();
    }
}
