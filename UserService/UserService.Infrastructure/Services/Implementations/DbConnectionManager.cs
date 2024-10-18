using UserService.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace UserService.Infrastructure.Services.Implementations;

public class DbConnectionManager : IDbConnectionManager
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbConnectionManager> _logger;
    private string NpgsqlConnectionString => _configuration["ConnectionString:DefaultConnection"];

    public DbConnectionManager(IConfiguration configuration, ILogger<DbConnectionManager> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private NpgsqlConnection PostgresDbConnection => new(NpgsqlConnectionString);

    public QueryFactory PostgresQueryFactory => new(PostgresDbConnection, new PostgresCompiler(), 60)
#if DEBUG
    {
        Logger = compiled => { _logger.LogInformation("Query = {@Query}", compiled.ToString()); }
    }
#endif
    ;
}