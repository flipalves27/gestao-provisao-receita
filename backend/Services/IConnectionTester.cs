using GestaoProvisao.Api.Models;

namespace GestaoProvisao.Api.Services;

public interface IConnectionTester
{
    /// <summary>
    /// Executa o diagnostico em etapas (rede, autenticacao, banco, app) medindo
    /// a latencia real de cada etapa.
    /// </summary>
    Task<ConnectionTestResult> TestAsync(ConnectionConfig config, CancellationToken ct = default);
}
