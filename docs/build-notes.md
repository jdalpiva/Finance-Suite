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
