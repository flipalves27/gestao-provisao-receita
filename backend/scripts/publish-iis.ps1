# Publica a API para hospedagem no IIS.
# Execute no PowerShell: .\scripts\publish-iis.ps1
# Opcional: .\scripts\publish-iis.ps1 -OutputPath "C:\inetpub\wwwroot\gestao-provisao-receita"

param(
    [string]$OutputPath = "C:\temp\gestao-provisao-receita"
)

$ErrorActionPreference = "Stop"
$backend = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $backend -Parent

Write-Host "Publicando para: $OutputPath"
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

Get-Process GestaoProvisao.Api -ErrorAction SilentlyContinue | Stop-Process -Force

Push-Location $backend
try {
    # Framework-dependent: usa o Hosting Bundle do IIS (SqlClient funciona; self-contained quebra o driver).
    dotnet publish -c Release -o $OutputPath
}
finally {
    Pop-Location
}

# Permissoes para o identity do Application Pool (grupo Users cobre ApplicationPoolIdentity local).
# SID S-1-5-32-545 = grupo Usuarios (funciona em Windows PT-BR).
icacls $OutputPath /grant "*S-1-5-32-545:(OI)(CI)M" /T | Out-Null

Write-Host ""
Write-Host "Concluido. No IIS, defina o caminho fisico da aplicacao para:"
Write-Host "  $OutputPath"
Write-Host ""
Write-Host "Application Pool: .NET CLR = Sem codigo gerenciado"
Write-Host "Reinicie o pool e acesse http://localhost/<alias-da-aplicacao>/"
Write-Host "Logs de erro do ANCM: $OutputPath\logs\stdout_*.log"
