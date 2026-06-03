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

## Publicar no IIS

1. Instale o **[ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)** (.NET 9).
2. Publique a API (não aponte o IIS só para o `index.html` da raiz do repo):

   ```powershell
   cd backend
   dotnet publish -c Release -o C:\inetpub\wwwroot\gestao-provisao-receita
   ```

3. No **Gerenciador do IIS**, crie um site ou aplicativo com o caminho físico da pasta publicada acima.
4. Application Pool: **.NET CLR = Sem código gerenciado**.
5. Conceda ao identity do pool **Leitura** na pasta publicada e **Modificar** em `App_Data` (connection string e chaves).
6. Evite hospedar dentro de `Documents` sem permissões extras; prefira `inetpub` ou outra pasta dedicada.

Se o site for uma **subaplicação** (ex.: `http://localhost/gestao-provisao-receita`), o frontend resolve as URLs da API em relação à URL atual. O backend usa `ASPNETCORE_APPL_PATH` (definido pelo módulo IIS) ou `PathBase` em `appsettings.json` se precisar forçar manualmente.

### Erro HTTP 500.30 (app failed to start)

Causas frequentes neste projeto:

1. **Caminho físico errado** — deve apontar para a pasta **publicada** (com `GestaoProvisao.Api.exe` e `web.config`), não para a raiz do repositório com só o `index.html`.
2. **Pasta em `Documents`** — o identity do Application Pool muitas vezes **não atravessa** `C:\Users\<você>\...`. Prefira `C:\temp\gestao-provisao-receita` ou `C:\inetpub\wwwroot\gestao-provisao-receita`.
3. **Permissão em `App_Data` e `logs`** — a API grava chaves em `App_Data/keys` na inicialização; o pool precisa de **Modificar** na pasta publicada.

Publicação recomendada (script):

```powershell
cd backend
.\scripts\publish-iis.ps1
# ou: .\scripts\publish-iis.ps1 -OutputPath "C:\inetpub\wwwroot\gestao-provisao-receita"
```

No IIS, caminho físico = pasta do script. Application Pool: **Sem código gerenciado**. Reinicie o pool.

Se ainda falhar, veja `logs\stdout_*.log` na pasta publicada (`stdoutLogEnabled` no `web.config`).

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
- **Somente leitura (defesa em camadas)**:
  - A query configurada em `Provisao:Query` passa pelo guard `SqlReadOnlyGuard`: precisa iniciar com `SELECT` ou `WITH` (CTE) e é rejeitada se contiver DML/DDL/execução (`INSERT`, `UPDATE`, `DELETE`, `MERGE`, `DROP`, `ALTER`, `CREATE`, `TRUNCATE`, `EXEC`/`EXECUTE`, `GRANT`, `REVOKE`, `INTO`, `sp_`, `xp_`) ou múltiplos statements (`;`).
  - A conexão é aberta com `ApplicationIntent=ReadOnly` (sinaliza intenção de leitura ao SQL Server / roteia para réplica em Always On).
  - O login do Application Pool recebe **apenas** `db_datareader` (sem `db_datawriter`/`db_ddladmin`) — ver `scripts/sql/grant-iis-apppool-login.sql`.
- **Validação de certificado por padrão**: `TrustServerCertificate=false` (valida o certificado do servidor). Habilite pela tela apenas em ambientes com certificado autoassinado conhecido. `Encrypt` e `Connection Timeout` também são controlados pela tela.
- **Sem vazamento de detalhes internos**: erros retornam mensagens genéricas ao cliente; a exceção completa é registrada apenas nos logs do servidor (handler global, `GET /api/provisao` e `POST /api/config/test`).
- Consultas **parametrizadas** via Dapper.
- A pasta `App_Data/` (e `bin/`, `obj/`, `publish/`, `*.dat`, `logs/`) está no `.gitignore` (segredos, chaves e artefatos de build não entram no controle de versão).

### Próximos passos de segurança

- A API **não possui autenticação**: qualquer um que alcance a URL pode salvar credenciais e ler dados. Recomenda-se proteger o acesso via **Windows Authentication no IIS** ou uma **API key** antes de expor o serviço.
- Em produção, considere um cofre de segredos gerenciado (Azure Key Vault, etc.).

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
