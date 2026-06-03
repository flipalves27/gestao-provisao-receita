using GestaoProvisao.Api.Models;
using Microsoft.Data.SqlClient;

namespace GestaoProvisao.Api.Services;

public interface ISqlConnectionFactory
{
    /// <summary>Monta o connection string a partir da config informada.</summary>
    string BuildConnectionString(ConnectionConfig config);

    /// <summary>Cria (sem abrir) uma SqlConnection a partir da config informada.</summary>
    SqlConnection Create(ConnectionConfig config);

    /// <summary>Cria (sem abrir) uma SqlConnection a partir da config salva. Lanca se nao configurada.</summary>
    SqlConnection CreateFromSaved();
}
