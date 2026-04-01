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

## Estabilidade de testes (Sprint 20 - 2026-04-01)

Sintoma observado:

- `dotnet test SMEFinanceSuite.sln` apresentou falha intermitente de runtime com `Fatal error. Internal CLR error. (0x80131506)`.

Diagnóstico pragmático:

- `dotnet build SMEFinanceSuite.sln` repetido ficou estável.
- `dotnet test` por projeto (`Unit` e `Integration`) em sequência ficou estável.
- O sintoma concentrou na execução agregada pela solution.

Mitigação adotada:

- Padronizar a execução da suíte via script `./scripts/test-stable.sh`.
- O script executa `Unit` e `Integration` de forma sequencial, com `-m:1` e `--disable-build-servers`.

Motivo da escolha:

- Mudança pequena, sem impacto no código de produção.
- Aumenta previsibilidade da suíte imediatamente.
- Evita abrir escopo de refatoração ampla de fixtures/infra sem evidência de necessidade.
