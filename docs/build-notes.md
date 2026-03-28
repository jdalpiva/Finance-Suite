# Build Notes

## Diagnóstico da base (2026-03-27)

Comandos executados:

```bash
dotnet --info
dotnet sln SMEFinanceSuite.sln list
dotnet restore SMEFinanceSuite.sln
dotnet build SMEFinanceSuite.sln
dotnet test SMEFinanceSuite.sln
```

Resultado:

- `dotnet sln ... list`: OK.
- `restore/build/test`: falharam com `NETSDK1045`.

Causa-raiz:

- A solution está em `net10.0` (via `Directory.Build.props`).
- O ambiente validado tinha apenas SDK `8.0.125`.
- O SDK 8 não suporta target `net10.0`.

## Ação necessária para ambiente saudável

Instalar SDK do .NET 10 e confirmar:

```bash
dotnet --list-sdks
```

Após isso, executar novamente:

```bash
dotnet restore SMEFinanceSuite.sln
dotnet build SMEFinanceSuite.sln
dotnet test SMEFinanceSuite.sln
```

## Observação

Foi adicionado `global.json` para explicitar o SDK esperado e reduzir divergência entre ambientes.

## Ajustes críticos pré-Sprint 1 (2026-03-28)

- Startup Avalonia: removido `GetAwaiter().GetResult()` e adotado fluxo assíncrono de inicialização.
- Dependências: `App.Desktop` não referencia mais `Core.Infrastructure` diretamente.
- Banco: inicialização passou para `Database.MigrateAsync()` com migration inicial versionada.

Comandos úteis para schema:

```bash
dotnet ef migrations add NomeDaMigration --project src/Core.Infrastructure --startup-project src/Core.Infrastructure
dotnet ef database update --project src/Core.Infrastructure --startup-project src/Core.Infrastructure
```
