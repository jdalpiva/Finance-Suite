# SMEFinanceSuite

Solução inicial real, desktop-first, para substituir planilhas por um sistema financeiro-operacional com interface gráfica, arquitetura clean pragmática e caminho de evolução para SaaS.

## Stack base

- **.NET 10 (LTS)** para runtime e SDK.
- **Avalonia UI** para interface desktop cross-platform.
- **Entity Framework Core + SQLite** para persistência local-first.
- **xUnit v3** para testes.

## Status atual

O app já cobre o fluxo principal desktop com:

- lançamentos financeiros com cadastro, edição, exclusão e filtros
- clientes
- produtos/serviços com status ativo/inativo
- relatórios com filtros, breakdowns, comparativo entre períodos e exportação CSV
- persistência local via SQLite

O foco atual do repositório esta em prontidão prática para uso real e pré-release leve, sem abrir novos módulos grandes.

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
./scripts/test-stable.sh
```

Para execução direta sem script, mantenha a suíte previsível executando os projetos de teste em sequência:

```bash
dotnet test tests/Unit/Unit.csproj -m:1 --disable-build-servers
dotnet test tests/Integration/Integration.csproj -m:1 --disable-build-servers
```

Se quiser validar rapidamente antes de rodar o desktop, este é o caminho operacional recomendado:

```bash
dotnet restore SMEFinanceSuite.sln
dotnet build SMEFinanceSuite.sln --no-restore
./scripts/test-stable.sh
```

## Como executar o desktop

```bash
dotnet run --project src/App.Desktop/App.Desktop.csproj
```

## Validação manual rápida

Checklist curto para smoke test local:

1. Abrir o app e confirmar carregamento sem erro na mensagem de status.
2. Cadastrar um cliente.
3. Cadastrar um produto/serviço.
4. Cadastrar um lançamento vinculado aos cadastros criados.
5. Editar e excluir registros, observando a confirmação em duas etapas nas ações destrutivas.
6. Abrir a aba de relatórios, aplicar filtros e exportar CSV.

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

## Notas operacionais

- O app usa confirmações em duas etapas para exclusões nos módulos principais.
- A exportação CSV fica disponível na aba `Relatórios`.
- Quando o banco estiver vazio, o desktop continua inicializando normalmente e os módulos exibem estados vazios de forma explícita.
