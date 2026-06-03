using System.Data;
using System.Diagnostics;
using System.Net.Sockets;
using Dapper;
using GestaoProvisao.Api.Models;
using Microsoft.Data.SqlClient;

namespace GestaoProvisao.Api.Services;

/// <summary>
/// Diagnostico de conectividade em quatro etapas, com latencia real:
///  1) net  - alcance TCP ao host/porta;
///  2) auth - abertura da conexao (autenticacao acontece no Open);
///  3) db   - contexto do banco (DB_NAME());
///  4) app  - consulta leve confirmando o pool de conexoes.
/// </summary>
public class ConnectionTester : IConnectionTester
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ILogger<ConnectionTester> _logger;

    public ConnectionTester(ISqlConnectionFactory factory, ILogger<ConnectionTester> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestAsync(ConnectionConfig config, CancellationToken ct = default)
    {
        var result = new ConnectionTestResult();
        var total = Stopwatch.StartNew();

        // ----- Etapa 1: alcance de rede (TCP) -----
        var (host, port) = ResolveHostPort(config);
        var swNet = Stopwatch.StartNew();
        try
        {
            using var tcp = new TcpClient();
            var connectTask = tcp.ConnectAsync(host, port);
            var timeout = TimeSpan.FromSeconds(config.ConnectionTimeout > 0 ? config.ConnectionTimeout : 30);
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeout, ct));
            swNet.Stop();

            if (completed != connectTask || !tcp.Connected)
            {
                result.Net.State = "off";
                result.Net.Ms = swNet.ElapsedMilliseconds;
                result.Net.Message = $"Servidor inacessivel em {host}:{port}";
                Finish(result, total, false, "Falha de rede: nao foi possivel alcancar o servidor.");
                return result;
            }

            result.Net.State = "on";
            result.Net.Ms = swNet.ElapsedMilliseconds;
            result.Net.Message = "Servidor acessivel";
        }
        catch (Exception ex)
        {
            swNet.Stop();
            result.Net.State = "off";
            result.Net.Ms = swNet.ElapsedMilliseconds;
            result.Net.Message = $"Falha ao alcancar {host}:{port}";
            _logger.LogWarning(ex, "Falha na etapa de rede do teste de conexao");
            Finish(result, total, false, "Falha de rede: " + ex.Message);
            return result;
        }

        // ----- Etapa 2: autenticacao (Open) -----
        SqlConnection? connection = null;
        var swAuth = Stopwatch.StartNew();
        try
        {
            connection = _factory.Create(config);
            await connection.OpenAsync(ct);
            swAuth.Stop();

            result.Auth.State = "on";
            result.Auth.Ms = swAuth.ElapsedMilliseconds;
            result.Auth.Message = string.Equals(config.AuthMode, "windows", StringComparison.OrdinalIgnoreCase)
                ? "Windows Auth OK"
                : "Login validado";
        }
        catch (SqlException ex)
        {
            swAuth.Stop();
            result.Auth.State = "off";
            result.Auth.Ms = swAuth.ElapsedMilliseconds;
            result.Auth.Message = "Falha de autenticacao";
            _logger.LogWarning(ex, "Falha na etapa de autenticacao do teste de conexao");
            connection?.Dispose();
            Finish(result, total, false, "Falha de autenticacao: " + ex.Message);
            return result;
        }

        try
        {
            // ----- Etapa 3: acesso ao banco -----
            var swDb = Stopwatch.StartNew();
            try
            {
                var dbName = await connection.ExecuteScalarAsync<string>(
                    new CommandDefinition("SELECT DB_NAME();", cancellationToken: ct));
                swDb.Stop();

                result.Db.State = "on";
                result.Db.Ms = swDb.ElapsedMilliseconds;
                result.Db.Message = string.IsNullOrEmpty(dbName) ? "Banco disponivel" : $"Banco '{dbName}' disponivel";
            }
            catch (SqlException ex)
            {
                swDb.Stop();
                result.Db.State = "off";
                result.Db.Ms = swDb.ElapsedMilliseconds;
                result.Db.Message = "Banco indisponivel";
                _logger.LogWarning(ex, "Falha na etapa de acesso ao banco do teste de conexao");
                Finish(result, total, false, "Falha no acesso ao banco: " + ex.Message);
                return result;
            }

            // ----- Etapa 4: conexao da aplicacao -----
            var swApp = Stopwatch.StartNew();
            try
            {
                await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition("SELECT 1;", cancellationToken: ct));
                swApp.Stop();

                result.App.State = "on";
                result.App.Ms = swApp.ElapsedMilliseconds;
                result.App.Message = "Pool de conexoes OK";
            }
            catch (SqlException ex)
            {
                swApp.Stop();
                result.App.State = "off";
                result.App.Ms = swApp.ElapsedMilliseconds;
                result.App.Message = "Falha na consulta de validacao";
                _logger.LogWarning(ex, "Falha na etapa de aplicacao do teste de conexao");
                Finish(result, total, false, "Falha na consulta da aplicacao: " + ex.Message);
                return result;
            }
        }
        finally
        {
            if (connection is not null)
                await connection.DisposeAsync();
        }

        Finish(result, total, true, "Conexao estabelecida com sucesso.");
        return result;
    }

    private static void Finish(ConnectionTestResult result, Stopwatch total, bool ok, string message)
    {
        total.Stop();
        result.TotalMs = total.ElapsedMilliseconds;
        result.Ok = ok;
        result.Message = message;
    }

    /// <summary>
    /// Extrai host e porta para o teste TCP. Suporta "host", "host,porta" e "host\instancia".
    /// Para instancias nomeadas usa a porta informada (ou 1433 como melhor esforco).
    /// </summary>
    private static (string Host, int Port) ResolveHostPort(ConnectionConfig config)
    {
        var server = (config.Server ?? string.Empty).Trim();
        var port = 1433;

        if (int.TryParse(config.Port?.Trim(), out var parsedPort) && parsedPort > 0)
            port = parsedPort;

        // "host,porta" tem prioridade sobre o campo Port.
        var commaIdx = server.IndexOf(',');
        if (commaIdx >= 0)
        {
            var portPart = server[(commaIdx + 1)..].Trim();
            if (int.TryParse(portPart, out var inlinePort) && inlinePort > 0)
                port = inlinePort;
            server = server[..commaIdx].Trim();
        }

        // Remove instancia nomeada ("host\SQLEXPRESS") para o teste TCP.
        var backslashIdx = server.IndexOf('\\');
        if (backslashIdx >= 0)
            server = server[..backslashIdx].Trim();

        if (string.IsNullOrWhiteSpace(server) ||
            server.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            server == ".")
        {
            server = "127.0.0.1";
        }

        return (server, port);
    }
}
