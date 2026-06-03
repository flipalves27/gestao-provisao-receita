using GestaoProvisao.Api.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Erros de validacao de modelo devolvem { message } (esperado pelo frontend),
// usando a primeira mensagem PT-BR definida via DataAnnotations no model.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? "Dados de conexao invalidos.";
        return new BadRequestObjectResult(new { message });
    };
});

// Data Protection: chaves persistidas em App_Data/keys para cifrar a connection string em repouso.
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
try
{
    Directory.CreateDirectory(keysDir);
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        $"Nao foi possivel criar a pasta de chaves em '{keysDir}'. " +
        "Conceda permissao de Modificar ao identity do Application Pool do IIS em App_Data/.", ex);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("GestaoProvisaoReceita");

builder.Services.AddSingleton<IConnectionConfigStore, ConnectionConfigStore>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IConnectionTester, ConnectionTester>();
builder.Services.AddScoped<IProvisaoRepository, ProvisaoRepository>();

var app = builder.Build();

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (ex is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("GlobalExceptionHandler");
            logger.LogError(ex, "Erro nao tratado ao processar {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "Erro interno do servidor." });
    });
});

// Subaplicacao no IIS (ex.: /gestao-provisao-receita): ASPNETCORE_APPL_PATH ou PathBase no appsettings.
var pathBase = builder.Configuration["PathBase"]
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");
if (!string.IsNullOrWhiteSpace(pathBase))
{
    pathBase = pathBase.TrimEnd('/');
    app.UsePathBase(pathBase);
}

// Serve o index.html (e demais assets) a partir de wwwroot, evitando CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
