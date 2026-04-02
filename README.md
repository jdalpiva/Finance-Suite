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

## Como gerar artefato de release desktop

O alvo principal pragmático atual e `linux-x64`, que tambem corresponde ao ambiente validado localmente para esta etapa de release.

Fluxo recomendado para gerar um diretório distribuível:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 --disable-parallel -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
```

Saída esperada:

- diretório final em `artifacts/desktop/linux-x64`
- executável em `artifacts/desktop/linux-x64/SMEFinanceSuite.Desktop`
- `appsettings.json` copiado junto do executável
- bibliotecas nativas e dependências do Avalonia presentes no mesmo diretório

Esse fluxo usa `restore + build` por RID para produzir o layout de distribuição validado na sprint atual.

Metadata básica de release aplicada nesta etapa:

- nome do produto: `SME Finance Suite`
- executável distribuível: `SMEFinanceSuite.Desktop`
- versão inicial de release: `0.27.0`

## Validação manual rápida

Checklist curto para smoke test local:

1. Abrir o app e confirmar carregamento sem erro na mensagem de status.
2. Cadastrar um cliente.
3. Cadastrar um produto/serviço.
4. Cadastrar um lançamento vinculado aos cadastros criados.
5. Editar e excluir registros, observando a confirmação em duas etapas nas ações destrutivas.
6. Abrir a aba de relatórios, aplicar filtros e exportar CSV.

Para validação de release, repita esse checklist usando o executável gerado em `artifacts/desktop/linux-x64`.

Guia curto para validar o artefato fora do workspace:

```bash
rm -rf /tmp/smefs-release-smoke
mkdir -p /tmp/smefs-release-smoke
cp -R artifacts/desktop/linux-x64 /tmp/smefs-release-smoke/app
HOME=/tmp/smefs-release-smoke/home /tmp/smefs-release-smoke/app/SMEFinanceSuite.Desktop
```

Resultado esperado:

- o artefato deve iniciar sem depender do workspace original
- `appsettings.json` deve estar no mesmo diretório do executável
- o banco SQLite relativo continua sendo resolvido para a pasta de dados do usuário
- em Linux sem sessão gráfica, o app agora falha com mensagem amigável orientando o uso em ambiente desktop com `DISPLAY` configurado

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
