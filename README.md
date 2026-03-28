# SMEFinanceSuite

Solução inicial real, desktop-first, para substituir planilhas por um sistema financeiro-operacional com interface gráfica, arquitetura clean pragmática e caminho de evolução para SaaS.

## Stack base

- **.NET 10 (LTS)** para runtime e SDK.
- **Avalonia UI** para interface desktop cross-platform.
- **Entity Framework Core + SQLite** para persistência local-first.
- **xUnit v3** para testes.

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

Por padrão, o app usa o arquivo SQLite local:

```text
Data Source=sme-finance-suite.db
```

O caminho pode ser alterado em `src/App.Desktop/appsettings.json`.

## Próximos passos sugeridos

1. Criar módulo de lançamentos com CRUD completo.
2. Criar módulo de clientes.
3. Criar módulo de produtos/serviços.
4. Adicionar orçamento e ponto de equilíbrio.
5. Expor a camada Application por uma API ASP.NET Core para iniciar a evolução para SaaS.
