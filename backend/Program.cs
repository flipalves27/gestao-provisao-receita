using GestaoProvisao.Api.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Data Protection: chaves persistidas em App_Data/keys para cifrar a connection string em repouso.
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("GestaoProvisaoReceita");

builder.Services.AddSingleton<IConnectionConfigStore, ConnectionConfigStore>();
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IConnectionTester, ConnectionTester>();
builder.Services.AddScoped<IProvisaoRepository, ProvisaoRepository>();

var app = builder.Build();

// Serve o index.html (e demais assets) a partir de wwwroot, evitando CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
