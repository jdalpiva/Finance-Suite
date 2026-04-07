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
3. `dpkg-deb` instalado para gerar o pacote `.deb` da Sprint 30.

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
- versão atual de release: `0.30.0`

## Como gerar pacote .deb

A forma pragmática adotada na Sprint 30 foi empacotar exatamente o release desktop `linux-x64` ja validado, sem trocar a arquitetura do produto nem abrir pipeline complexa.

Layout do pacote:

- pacote Debian: `sme-finance-suite`
- versão: derivada de `src/App.Desktop/App.Desktop.csproj`
- diretório instalado do app: `/opt/sme-finance-suite`
- launcher no PATH: `/usr/bin/sme-finance-suite`
- desktop entry: `/usr/share/applications/sme-finance-suite.desktop`
- icone dedicado: fora do escopo desta sprint

Comando recomendado:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 --disable-parallel -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
./scripts/package-deb.sh
```

Saída esperada:

- pacote em `artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`
- staging temporário em `artifacts/deb/staging/`

Observações importantes:

- o script empacota o artefato ja gerado em `artifacts/desktop/linux-x64`
- separar build e empacotamento reduz risco operacional e mantém o fluxo alinhado ao release `linux-x64` ja validado
- o pacote redistribui o mesmo layout `linux-x64` ja aprovado no smoke manual; ele apenas instala esse layout em um destino mais amigável para Ubuntu/Linux desktop
- a sprint não inclui AppImage, multiplos formatos ou CI/CD de empacotamento

## Validação manual rápida

Checklist curto para smoke test local:

1. Abrir o app e confirmar carregamento sem erro na mensagem de status.
2. Cadastrar um cliente.
3. Cadastrar um produto/serviço.
4. Cadastrar um lançamento vinculado aos cadastros criados.
5. Editar e excluir registros, observando a confirmação em duas etapas nas ações destrutivas.
6. Abrir a aba de relatórios, aplicar filtros e exportar CSV.

Para validação de release, repita esse checklist usando o executável gerado em `artifacts/desktop/linux-x64`.

Checklist curto para o `.deb`:

1. Gerar o pacote com `./scripts/package-deb.sh`.
2. Inspecionar o conteúdo com `dpkg-deb --contents artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`.
3. Instalar em um Ubuntu desktop real com `sudo dpkg -i artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`.
4. Abrir pelo menu de aplicações ou executando `sme-finance-suite`.
5. Confirmar startup, leitura de configuração e persistência local no diretório do usuário.

Validação executada na Sprint 31:

- tentativa de instalação sistêmica com `sudo dpkg -i` bloqueada por autenticação de superusuário neste ambiente
- conteúdo do pacote validado com `dpkg-deb --info` e `dpkg-deb --contents`
- `sme-finance-suite.desktop` validado com `desktop-file-validate`
- startup gráfico validado em sessão Ubuntu GNOME real a partir do conteúdo extraído do `.deb`, com janela `SME Finance Suite`
- persistência local observada em `~/.local/share/SMEFinanceSuite/sme-finance-suite.db`
- seed inicial confirmada no SQLite local: `1` cliente, `2` produtos/serviços e `4` lançamentos

Limite conhecido dessa validação:

- sem autenticação de root disponível para o agente, a instalação sistêmica real, o launcher final em `/usr/bin` e a desinstalação via `dpkg -r` permanecem para confirmação manual no Ubuntu alvo

Validação pragmática sem instalar no sistema, útil neste ambiente:

```bash
rm -rf /tmp/smefs-deb-smoke
mkdir -p /tmp/smefs-deb-smoke/root
dpkg-deb -x artifacts/packages/sme-finance-suite_0.30.0_amd64.deb /tmp/smefs-deb-smoke/root
HOME=/tmp/smefs-deb-smoke/home /tmp/smefs-deb-smoke/root/opt/sme-finance-suite/SMEFinanceSuite.Desktop
```

Resultado esperado:

- o `.deb` expande o app em `/opt/sme-finance-suite`
- o launcher instalável fica disponível em `/usr/bin/sme-finance-suite`
- o desktop entry fica disponível em `/usr/share/applications/sme-finance-suite.desktop`
- a execucao fora do workspace preserva a leitura de `appsettings.json`
- a persistencia relativa continua sendo resolvida para a pasta local de dados do usuário
- em ambiente sem sessão gráfica, a falha continua amigável e consistente com o release validado anteriormente

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

Para um smoke totalmente isolado em `/tmp`, sem reutilizar a pasta padrão de dados do usuário, ajuste o `appsettings.json` copiado para usar um caminho absoluto de SQLite:

```bash
mkdir -p /tmp/smefs-release-smoke/data
perl -0pi -e 's#Data Source=sme-finance-suite\.db#Data Source=/tmp/smefs-release-smoke/data/smoke-release.db#g' /tmp/smefs-release-smoke/app/appsettings.json
DISPLAY=:0 WAYLAND_DISPLAY= HOME=/tmp/smefs-release-smoke/home /tmp/smefs-release-smoke/app/SMEFinanceSuite.Desktop
```

Smoke gráfico validado na Sprint 28:

- o executável publicado abre fora do workspace de desenvolvimento
- o título da janela sobe como `SME Finance Suite`
- a leitura de `appsettings.json` foi confirmada apontando o SQLite para um caminho absoluto dentro de `/tmp`
- a inicialização cria o banco isolado com schema e seed esperados (`1` cliente, `2` produtos/serviços e `4` lançamentos iniciais)
- a validação visual aprofundada de cadastros, navegação entre abas e exportação CSV ainda deve ser concluída manualmente em uma sessão desktop limpa, porque a automação de entrada por `xdotool` ficou inconclusiva neste ambiente

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
