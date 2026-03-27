# SMEFinanceSuite

Solução inicial real, desktop-first, para substituir planilhas por um sistema financeiro-operacional com interface gráfica, arquitetura clean pragmática e caminho de evolução para SaaS.

## Stack base

- **.NET 10 (LTS)** para o runtime e SDK.
- **Avalonia UI** para a interface desktop cross-platform.
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

## Como abrir e rodar

```bash
dotnet restore
dotnet build
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

## Observação importante

Esta solução foi montada estruturalmente e não foi validada por build dentro deste ambiente, porque o container usado para gerar os arquivos **não possui o SDK do .NET instalado**. Ainda assim, os arquivos foram organizados para abrir como solução e servir de base real de desenvolvimento.
