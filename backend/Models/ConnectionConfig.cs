namespace GestaoProvisao.Api.Models;

/// <summary>
/// Dados de conexao informados pela tela de Configuracoes.
/// Espelha os campos do formulario em index.html (view-config).
/// A validacao e feita de forma centralizada em ConfigController.Validate
/// (mensagens em PT-BR + regra condicional de autenticacao).
/// </summary>
public class ConnectionConfig
{
    public string Server { get; set; } = string.Empty;

    public string? Port { get; set; } = "1433";

    public string Database { get; set; } = string.Empty;

    /// <summary>"sql" (SQL Server Authentication) ou "windows" (Trusted Connection).</summary>
    public string AuthMode { get; set; } = "sql";

    public string? User { get; set; }

    public string? Password { get; set; }

    public bool Encrypt { get; set; } = true;

    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>Timeout de conexao em segundos.</summary>
    public int ConnectionTimeout { get; set; } = 30;
}
