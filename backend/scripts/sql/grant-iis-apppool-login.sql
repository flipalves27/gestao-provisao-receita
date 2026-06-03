-- Execute no SSMS conectado como administrador do SQL (ex.: FELIPE-DELL\felip).
-- Concede acesso ao identity do Application Pool do IIS.
-- Ajuste o nome do login se o pool no IIS for diferente de "gestao-provisao-receita".

USE [master];
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'IIS APPPOOL\gestao-provisao-receita')
    CREATE LOGIN [IIS APPPOOL\gestao-provisao-receita] FROM WINDOWS;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'gestao-provisao-receita')
    CREATE DATABASE [gestao-provisao-receita];
GO

USE [gestao-provisao-receita];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'IIS APPPOOL\gestao-provisao-receita')
    CREATE USER [IIS APPPOOL\gestao-provisao-receita] FOR LOGIN [IIS APPPOOL\gestao-provisao-receita];
GO

-- Privilégio mínimo: a API é somente-leitura, então concede apenas db_datareader.
ALTER ROLE [db_datareader] ADD MEMBER [IIS APPPOOL\gestao-provisao-receita];
GO
