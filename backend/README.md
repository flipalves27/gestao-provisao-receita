# Gestão de Provisão de Receita — Backend (ASP.NET Core)

API em ASP.NET Core que conecta ao **SQL Server** e serve o frontend (`index.html`).
Como navegadores não conectam diretamente ao SQL Server, esta camada intermediária:

- monta e **persiste a connection string cifrada** (ASP.NET Data Protection);
- expõe um **teste de conexão em etapas** com latências reais (rede, autenticação, banco, app);
- consulta os registros de provisão via **Dapper** com uma query **parametrizável** em `appsettings.json`;
- serve o `index.html` em `wwwroot`, evitando CORS.

## Requisitos

- **.NET SDK 9.0** (o projeto tem como alvo `net9.0`).
  > O plano original mencionava .NET 8; o alvo foi ajustado para 9.0 por ser o SDK instalado nesta máquina. Para usar .NET 8, troque `<TargetFramework>net8.0</TargetFramework>` no `.csproj` (requer o runtime 8 instalado).
- Acesso de rede a uma instância SQL Server.

## Como rodar

Na pasta `backend/`:

```bash
dotnet run
```

A API sobe (por padrão em `http://localhost:5080`) e **serve o frontend** na raiz. Acesse:

```
http://localhost:5080/
```

O passo de build copia automaticamente o `index.html` da raiz do repositório para `wwwroot/` (alvo MSBuild `CopyFrontend`), então edite sempre o `index.html` da raiz.

## Fluxo de uso

1. Abra a aplicação e vá em **Configurações**.
2. Preencha servidor, banco, modo de autenticação e credenciais.
3. Clique em **Testar conexão** (diagnóstico em etapas com latência real).
4. Clique em **Salvar configuração** (grava a connection string cifrada em `App_Data/connection.dat`).
5. Volte para **Provisão de Receita**; os dados são carregados de `GET /api/provisao`.
   - Sem registros em aberto, a tabela exibe um **estado vazio** ("Nenhum registro em aberto").
   - Se o backend não estiver configurado/acessível, a tabela exibe um **estado de erro** com a mensagem e um atalho para **Configurações** (não há mais dados de exemplo embutidos).

## Endpoints

| Método | Rota                 | Descrição                                                            |
|--------|----------------------|----------------------------------------------------------------------|
| GET    | `/api/config`        | Config salva com **senha mascarada** + flag `isConfigured`.          |
| POST   | `/api/config`        | Valida, monta e **salva** a connection string cifrada.               |
| POST   | `/api/config/test`   | Testa em etapas: `{ net, auth, db, app, totalMs, ok, message }`.     |
| GET    | `/api/provisao`      | Retorna `ProvisaoRecord[]`. `409` se não configurado.                |

## Configuração da consulta (`Provisao:Query`)

O schema real do banco é desconhecido, portanto a consulta é **parametrizável** em `appsettings.json`.
As colunas retornadas devem casar (case-insensitive) com as propriedades de `ProvisaoRecord`:
`Venc`, `VencOrig`, `Dias`, `Filial`, `Apolice`, `Endosso`, `Grupo`, `Nome`, `Parcela`, `VlTotal`, `Prorr`, `Email`, `Fone`, `Resp`, `Cnpj`.

Exemplo (ajuste tabelas/colunas ao seu banco):

```json
{
  "Provisao": {
    "CommandTimeoutSeconds": 60,
    "Query": "SELECT CONVERT(varchar(10), p.Vencimento, 103) AS Venc, CONVERT(varchar(10), p.VencimentoOriginal, 103) AS VencOrig, DATEDIFF(day, p.Vencimento, GETDATE()) AS Dias, p.Filial AS Filial, p.Apolice AS Apolice, p.Endosso AS Endosso, p.GrupoEconomico AS Grupo, p.Tomador AS Nome, p.Parcela AS Parcela, p.ValorTotal AS VlTotal, p.Prorrogacoes AS Prorr, c.Email AS Email, c.Telefone AS Fone, c.Responsavel AS Resp, c.Cnpj AS Cnpj FROM dbo.ProvisaoReceita p LEFT JOIN dbo.Contato c ON c.TomadorId = p.TomadorId WHERE p.Situacao = 'EM_ABERTO' ORDER BY p.Vencimento;"
  }
}
```

Notas sobre a query:

- `Venc`/`VencOrig` saem como string no formato `dd/MM/yyyy` (`CONVERT(..., 103)`), igual ao que o frontend exibe.
- `Dias` pode ser calculado com `DATEDIFF(day, Vencimento, GETDATE())`.
- Mantenha o uso de parâmetros (`@param`) caso adicione filtros dinâmicos — **nunca** concatene entrada do usuário.

## Segurança

- **Senha cifrada em repouso**: a connection string é protegida com `IDataProtector` e gravada em `App_Data/connection.dat`. As chaves de proteção ficam em `App_Data/keys/`.
- A senha **nunca** é retornada em texto plano: `GET /api/config` devolve apenas uma máscara (`passwordMask`).
- Consultas **parametrizadas** via Dapper; `Connection Timeout` configurável; `Encrypt`/`TrustServerCertificate` controlados pela tela.
- A pasta `App_Data/` está no `.gitignore` (segredos e chaves não entram no controle de versão).
- Em produção, considere um cofre de segredos gerenciado (Azure Key Vault, etc.) e proteja o acesso à API.

## Estrutura

```
backend/
├─ GestaoProvisao.Api.csproj
├─ Program.cs                 # DI, Data Protection, static files
├─ appsettings.json           # Provisao:Query (ajustar ao schema real)
├─ Controllers/               # ConfigController, ProvisaoController
├─ Models/                    # ConnectionConfig, ConnectionTestResult, ProvisaoRecord
├─ Services/                  # store, factory, tester, repository
└─ wwwroot/                   # index.html copiado da raiz no build
```
