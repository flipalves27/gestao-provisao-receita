using GestaoProvisao.Api.Models;

namespace GestaoProvisao.Api.Services;

public interface IProvisaoRepository
{
    /// <summary>
    /// Consulta os registros de provisao usando a query configurada em Provisao:Query.
    /// </summary>
    Task<IReadOnlyList<ProvisaoRecord>> GetAllAsync(CancellationToken ct = default);
}
