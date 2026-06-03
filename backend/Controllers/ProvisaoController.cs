using GestaoProvisao.Api.Models;
using GestaoProvisao.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoProvisao.Api.Controllers;

[ApiController]
[Route("api/provisao")]
public class ProvisaoController : ControllerBase
{
    private readonly IProvisaoRepository _repository;
    private readonly IConnectionConfigStore _store;
    private readonly ILogger<ProvisaoController> _logger;

    public ProvisaoController(
        IProvisaoRepository repository,
        IConnectionConfigStore store,
        ILogger<ProvisaoController> logger)
    {
        _repository = repository;
        _store = store;
        _logger = logger;
    }

    /// <summary>Retorna os registros de provisao de receita.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProvisaoRecord>>> Get(CancellationToken ct)
    {
        if (!_store.IsConfigured)
        {
            return StatusCode(StatusCodes.Status409Conflict,
                new { message = "Conexao nao configurada. Configure em Configuracoes antes de carregar os dados." });
        }

        try
        {
            var records = await _repository.GetAllAsync(ct);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar provisao de receita");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Falha ao consultar o banco de dados: " + ex.Message });
        }
    }
}
