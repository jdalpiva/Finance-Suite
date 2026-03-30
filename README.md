# SMEFinanceSuite

Solução inicial real, desktop-first, para substituir planilhas por um sistema financeiro-operacional com interface gráfica, arquitetura clean pragmática e caminho de evolução para SaaS.

## Stack base

- **.NET 10 (LTS)** para runtime e SDK.
- **Avalonia UI** para interface desktop cross-platform.
- **Entity Framework Core + SQLite** para persistência local-first.
- **xUnit v3** para testes.

## Status das sprints

- **Sprint 1 (concluída)**: vertical slice de lançamentos financeiros com **Register + List**.
- **Fora de escopo da Sprint 1**: **Update** e **Delete** de lançamentos.
- **Sprint 2 (iniciada)**: evolução da UI de lançamentos (listagem, cadastro e filtros por período) integrada ao `IFinancialEntryService`.

## Estrutura

```text
SMEFinanceSuite/
├── docs/
├── src/
│   ├── App.Desktop/
│   ├── App.Bootstrapper/
│   ├── Core.Domain/
│   ├── Core.Application/
│   └── Core.Infrastructure/
└── tests/
    ├── Unit/
    └── Integration/
```

## Pré-requisitos

1. SDK do .NET 10 instalado (o repositório está fixado por `global.json`).
2. Sistema operacional Linux, Windows ou macOS com suporte ao .NET SDK.

Valide o SDK ativo:

```bash
dotnet --list-sdks
```

## Como restaurar, compilar e testar

```bash
dotnet restore SMEFinanceSuite.sln
dotnet build SMEFinanceSuite.sln
dotnet test SMEFinanceSuite.sln
```

## Como executar o desktop

```bash
dotnet run --project src/App.Desktop/App.Desktop.csproj
```

## Banco de dados

Por padrão, o app usa SQLite com nome de arquivo configurado em `appsettings.json`:

```text
Data Source=sme-finance-suite.db
```

No desktop, quando o `Data Source` for relativo, o caminho final é resolvido de forma determinística para a pasta de dados do usuário (`LocalApplicationData/SMEFinanceSuite`).

O connection string base pode ser alterado em `src/App.Desktop/appsettings.json`.

### Migrations (EF Core)

O projeto usa migrations para evolução de schema (não usa `EnsureCreated`).

Aplicar migrations no banco configurado:

```bash
dotnet ef database update --project src/Core.Infrastructure --startup-project src/Core.Infrastructure
```

Criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration --project src/Core.Infrastructure --startup-project src/Core.Infrastructure
```

## Próximos passos sugeridos

1. Expandir o módulo de lançamentos com Update/Delete mantendo regras no domínio.
2. Criar módulo de clientes.
3. Criar módulo de produtos/serviços.
4. Adicionar orçamento e ponto de equilíbrio.
5. Expor a camada Application por uma API ASP.NET Core para iniciar a evolução para SaaS.
