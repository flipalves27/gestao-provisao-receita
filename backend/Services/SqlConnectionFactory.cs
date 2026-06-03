using GestaoProvisao.Api.Models;
using Microsoft.Data.SqlClient;

namespace GestaoProvisao.Api.Services;

/// <summary>
/// Monta o connection string autoritativo (no servidor) a partir da config,
/// suportando autenticacao SQL e Windows, Encrypt, TrustServerCertificate e timeout.
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly IConnectionConfigStore _store;

    public SqlConnectionFactory(IConnectionConfigStore store)
    {
        _store = store;
    }

    public string BuildConnectionString(ConnectionConfig config)
    {
        var dataSource = config.Server.Trim();
        if (!string.IsNullOrWhiteSpace(config.Port) && config.Port.Trim() != "1433")
        {
            // Porta customizada: SqlClient usa "host,porta".
            dataSource = $"{dataSource},{config.Port.Trim()}";
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = dataSource,
            InitialCatalog = config.Database.Trim(),
            Encrypt = config.Encrypt,
            TrustServerCertificate = config.TrustServerCertificate,
            ConnectTimeout = config.ConnectionTimeout > 0 ? config.ConnectionTimeout : 30,
            ApplicationName = "GestaoProvisaoReceita",
            // Backend e somente-leitura: sinaliza intencao de leitura ao SQL Server
            // (roteia para replica secundaria em Always On, quando disponivel).
            ApplicationIntent = ApplicationIntent.ReadOnly,
        };

        if (string.Equals(config.AuthMode, "windows", StringComparison.OrdinalIgnoreCase))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = config.User ?? string.Empty;
            builder.Password = config.Password ?? string.Empty;
        }

        return builder.ConnectionString;
    }

    public SqlConnection Create(ConnectionConfig config)
        => new(BuildConnectionString(config));

    public SqlConnection CreateFromSaved()
    {
        var config = _store.Load()
            ?? throw new InvalidOperationException("Nenhuma configuracao de conexao foi salva. Configure em /api/config.");
        return Create(config);
    }
}
