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

## Release desktop pragmático (Sprint 26 - 2026-04-02)

Objetivo adotado:

- preparar um artefato desktop distribuível sem abrir pipeline complexa nem empacotamento multi-plataforma nesta etapa.

Decisão pragmática:

- alvo principal atual: `linux-x64`
- distribuição inicial: diretório executável gerado por RID-specific build
- motivo: o layout resultante ja inclui `SMEFinanceSuite.Desktop`, `runtimeconfig`, `deps`, `appsettings.json` e dependências nativas necessárias

Comando recomendado:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 --disable-parallel -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
```

Saída:

- `artifacts/desktop/linux-x64`

Validação executada na sprint:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
```

Observação importante:

- `dotnet publish` puro apresentou falha opaca no target de restore neste ambiente, sem mensagem diagnóstica útil.
- Como mitigação de baixo risco, o fluxo oficial desta sprint ficou padronizado em comandos diretos de `restore + build -r linux-x64`.
- O resultado continua adequado para distribuição local e validação manual do desktop app.

## Validação pragmática de release (Sprint 27 - 2026-04-02)

Escopo validado:

- artefato copiado para `/tmp/smefs-release-smoke/app`, fora do workspace de desenvolvimento
- presença confirmada de executável ELF, `appsettings.json`, `runtimeconfig` e dependências nativas
- identificação básica de release adicionada ao projeto desktop:
  - `Product`: `SME Finance Suite`
  - executável: `SMEFinanceSuite.Desktop`
  - versão: `0.27.0`

Comandos usados na validação:

```bash
file artifacts/desktop/linux-x64/SMEFinanceSuite.Desktop
rm -rf /tmp/smefs-release-smoke
mkdir -p /tmp/smefs-release-smoke
cp -R artifacts/desktop/linux-x64 /tmp/smefs-release-smoke/app
HOME=/tmp/smefs-release-smoke/home /tmp/smefs-release-smoke/app/SMEFinanceSuite.Desktop
```

Resultado observado:

- o diretório publicado permanece executável fora do workspace original
- a leitura de configuração continua viável porque `appsettings.json` segue junto do artefato
- em ambiente Linux sem sessão gráfica, a tentativa de abrir a UI falha antes da janela carregar
- a sprint tratou esse caso com mensagem amigável de startup, substituindo o stack trace cru anterior de `XOpenDisplay failed`

Lacuna que permanece conhecida:

- a abertura visual completa do app ainda precisa ser validada em uma sessão gráfica Linux real, fora do ambiente de desenvolvimento

## Validação gráfica pragmática (Sprint 28 - 2026-04-02)

Objetivo da etapa:

- validar o artefato publicado em sessão gráfica real, fora do workspace de desenvolvimento, antes de avançar para empacotamento mais amigável

Procedimento efetivamente validado:

```bash
rm -rf /tmp/smefs-release-smoke
mkdir -p /tmp/smefs-release-smoke/data
cp -R artifacts/desktop/linux-x64 /tmp/smefs-release-smoke/app
perl -0pi -e 's#Data Source=sme-finance-suite\.db#Data Source=/tmp/smefs-release-smoke/data/smoke-release.db#g' /tmp/smefs-release-smoke/app/appsettings.json
DISPLAY=:0 WAYLAND_DISPLAY= HOME=/tmp/smefs-release-smoke/home /tmp/smefs-release-smoke/app/SMEFinanceSuite.Desktop
```

Resultado validado:

- o executável abre em sessão gráfica real com o título `SME Finance Suite`
- a inicialização ocorre sem stderr relevante
- o `appsettings.json` copiado foi realmente lido, porque a connection string absoluta criou o banco em `/tmp/smefs-release-smoke/data/smoke-release.db`
- o banco isolado foi inicializado corretamente fora do workspace, com seed e schema esperados:
  - `customers`: `1`
  - `products_services`: `2`
  - `financial_entries`: `4`

Achado operacional relevante:

- para smoke totalmente isolado em `/tmp`, usar apenas `HOME=/tmp/...` não foi suficiente para redirecionar a persistência do caminho relativo configurado por padrão
- como mitigação pragmática de validação, a sprint passou a documentar o override temporário da connection string no `appsettings.json` copiado
- isso não altera o comportamento oficial do produto: em uso normal, o caminho relativo continua sendo resolvido para a pasta local de dados do usuário

Limitação remanescente:

- a automação de entrada completa por `xdotool` ficou inconclusiva neste ambiente, então os fluxos de cadastro/edição/navegação detalhada e a exportação CSV ainda devem ser fechados com smoke manual em uma sessão desktop limpa

## Empacotamento Debian pragmático (Sprint 30 - 2026-04-06)

Objetivo da etapa:

- transformar o release atual em uma distribuicao mais amigavel para Ubuntu/Linux desktop sem alterar a arquitetura do produto

Decisão adotada:

- formato unico nesta sprint: `.deb`
- estratégia: empacotar o mesmo diretório `artifacts/desktop/linux-x64` ja validado nas sprints anteriores
- ferramenta: `dpkg-deb`
- instalação do app em `/opt/sme-finance-suite`
- launcher no sistema em `/usr/bin/sme-finance-suite`
- desktop entry em `/usr/share/applications/sme-finance-suite.desktop`
- ícone dedicado ficou fora do escopo por não haver asset pronto e por não ser necessário para validar distribuição real

Motivo da escolha:

- reaproveita integralmente o release `linux-x64` ja conhecido
- evita introduzir pipeline paralela, formato adicional ou refactor de runtime
- mantém o risco baixo porque o pacote só reorganiza a entrega do artefato existente

Implementação mínima adicionada:

- `scripts/package-deb.sh`
- `packaging/deb/control.template`
- `packaging/deb/launcher.template`
- `packaging/deb/sme-finance-suite.desktop`

Versão de release aplicada:

- `0.30.0`

Comando oficial:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 --disable-parallel -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
./scripts/package-deb.sh
```

Comportamento do script:

- assume que o artefato `artifacts/desktop/linux-x64` ja foi gerado pelo fluxo oficial de release
- monta a árvore Debian temporária em `artifacts/deb/staging/`
- copia o artefato atual para `/opt/sme-finance-suite`
- gera o pacote em `artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`

Validação pragmática executável neste ambiente:

```bash
dotnet restore src/App.Desktop/App.Desktop.csproj -r linux-x64 --disable-parallel -v minimal
dotnet build src/App.Desktop/App.Desktop.csproj -c Release -r linux-x64 --no-restore --disable-build-servers -v minimal -o artifacts/desktop/linux-x64
./scripts/package-deb.sh
dpkg-deb --contents artifacts/packages/sme-finance-suite_0.30.0_amd64.deb
rm -rf /tmp/smefs-deb-smoke
mkdir -p /tmp/smefs-deb-smoke/root
dpkg-deb -x artifacts/packages/sme-finance-suite_0.30.0_amd64.deb /tmp/smefs-deb-smoke/root
HOME=/tmp/smefs-deb-smoke/home /tmp/smefs-deb-smoke/root/opt/sme-finance-suite/SMEFinanceSuite.Desktop
```

Resultado esperado:

- o conteúdo do pacote fica limitado ao layout mínimo de distribuição
- o app continua abrindo fora do workspace pelo binário instalado em `/opt`
- `appsettings.json` segue sendo lido a partir do diretório do executável
- a persistência relativa continua sendo resolvida para `LocalApplicationData/SMEFinanceSuite`
- em ambiente sem sessão gráfica, a mensagem amigável de startup permanece igual ao release anterior

Limite assumido nesta sprint:

- a instalação real via `sudo dpkg -i ...` e a abertura pelo menu gráfico devem ser confirmadas no Ubuntu desktop alvo, porque este ambiente não executa instalação sistêmica nem smoke visual completo

## Validação do pacote instalado no alvo Ubuntu (Sprint 31 - 2026-04-06)

Objetivo da etapa:

- validar a instalação real do `.deb` e o uso básico do app em sessão gráfica Ubuntu, corrigindo apenas problemas concretos de baixo risco

Resultado da instalação sistêmica:

- tentativa executada com `sudo dpkg -i artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`
- bloqueio observado: autenticação de superusuário indisponível para o agente neste ambiente
- conclusão: não houve erro concreto do pacote na fase de instalação; a limitação ficou em credencial de root, não no `.deb`

Validação realmente concluída nesta sprint:

- `dpkg-deb --info artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`: OK
- `dpkg-deb --contents artifacts/packages/sme-finance-suite_0.30.0_amd64.deb`: OK
- `desktop-file-validate` no `sme-finance-suite.desktop`: OK
- abertura gráfica do app em sessão Ubuntu GNOME real a partir da árvore extraída do pacote: OK
- janela confirmada com título `SME Finance Suite`
- geometria observada da janela:
  - largura: `1170`
  - altura: `807`
- persistência local confirmada em `~/.local/share/SMEFinanceSuite/sme-finance-suite.db`
- seed inicial confirmada no banco local:
  - `customers`: `1`
  - `products_services`: `2`
  - `financial_entries`: `4`

Achado operacional relevante:

- em Linux desktop, trocar apenas `HOME` no processo não foi suficiente para isolar a persistência em `LocalApplicationData`; o comportamento observado permaneceu coerente com a nota já documentada na Sprint 28

Correções de produto nesta sprint:

- nenhuma alteração de código foi necessária, porque não apareceu defeito concreto de instalação ou execução do pacote

Pontos que permanecem pendentes para fechamento final do ciclo:

- instalação sistêmica real com credencial de root
- validação do launcher final em `/usr/bin/sme-finance-suite`
- validação do desktop entry pelo menu do sistema após instalação
- desinstalação real com `dpkg -r`
