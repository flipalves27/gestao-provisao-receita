using GestaoProvisao.Api.Models;
using GestaoProvisao.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestaoProvisao.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly IConnectionConfigStore _store;
    private readonly IConnectionTester _tester;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        IConnectionConfigStore store,
        IConnectionTester tester,
        ILogger<ConfigController> logger)
    {
        _store = store;
        _tester = tester;
        _logger = logger;
    }

    /// <summary>Config salva com senha mascarada + flag isConfigured.</summary>
    [HttpGet]
    public ActionResult<ConnectionConfigView> Get()
    {
        var view = _store.LoadMasked();
        if (view is null)
            return Ok(new ConnectionConfigView { IsConfigured = false });

        return Ok(view);
    }

    /// <summary>Valida (via ModelState) e salva a configuracao cifrada.</summary>
    [HttpPost]
    public ActionResult Save([FromBody] ConnectionConfig config)
    {
        _store.Save(config);
        _logger.LogInformation("Configuracao de conexao salva para o servidor {Server}", config.Server);
        return Ok(new { ok = true, message = "Configuracao salva com sucesso." });
    }

    /// <summary>Testa a conexao em etapas e retorna as latencias reais.</summary>
    [HttpPost("test")]
    public async Task<ActionResult<ConnectionTestResult>> Test(
        [FromBody] ConnectionConfig config,
        CancellationToken ct)
    {
        try
        {
            var result = await _tester.TestAsync(config, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no teste de conexao");
            return StatusCode(500, new { message = "Erro inesperado ao testar a conexao. Consulte os logs do servidor para detalhes." });
        }
    }
}
