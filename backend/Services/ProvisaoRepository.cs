using Dapper;
using GestaoProvisao.Api.Models;

namespace GestaoProvisao.Api.Services;

/// <summary>
/// Le os registros de provisao com Dapper. A consulta SQL e parametrizavel via
/// appsettings (chave "Provisao:Query"), pois o schema real do cliente e desconhecido.
/// </summary>
public class ProvisaoRepository : IProvisaoRepository
{
    private readonly ISqlConnectionFactory _factory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProvisaoRepository> _logger;

    public ProvisaoRepository(
        ISqlConnectionFactory factory,
        IConfiguration configuration,
        ILogger<ProvisaoRepository> logger)
    {
        _factory = factory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProvisaoRecord>> GetAllAsync(CancellationToken ct = default)
    {
        var query = _configuration["Provisao:Query"];
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException(
                "A consulta nao esta configurada. Defina 'Provisao:Query' em appsettings.json.");
        }

        SqlReadOnlyGuard.EnsureReadOnly(query);

        var commandTimeout = _configuration.GetValue<int?>("Provisao:CommandTimeoutSeconds") ?? 60;

        await using var connection = _factory.CreateFromSaved();

        var command = new CommandDefinition(
            query,
            commandTimeout: commandTimeout,
            cancellationToken: ct);

        var rows = await connection.QueryAsync<ProvisaoRecord>(command);
        return rows.AsList();
    }
}
