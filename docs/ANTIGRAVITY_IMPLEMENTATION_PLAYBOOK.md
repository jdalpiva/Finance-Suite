# SMEFinanceSuite — Playbook para Antigravity + Ubuntu 25.10

## Objetivo

Construir um software financeiro-operacional para pequenas e médias empresas, substituindo planilhas por uma aplicação desktop intuitiva, confiável e preparada para futura evolução para SaaS.

---

## 1. Princípios de engenharia da computação aplicados

### 1.1 Decomposição em camadas
Separar a solução em:
- **Domain**: regras centrais do negócio.
- **Application**: contratos e casos de uso.
- **Infrastructure**: persistência, acesso a dados e integrações.
- **Desktop**: interface gráfica e fluxo do usuário.
- **Bootstrapper**: composição da aplicação.

### 1.2 Acoplamento baixo e coesão alta
- Cada projeto deve ter um motivo claro para mudar.
- Regras de negócio não devem depender da interface.
- Infraestrutura não deve contaminar o domínio com detalhes técnicos.

### 1.3 Pensamento de sistema
O produto precisa ser desenhado como um sistema vivo:
- dados consistentes;
- rastreabilidade;
- previsibilidade operacional;
- observabilidade mínima;
- capacidade de evolução.

### 1.4 Restrições reais
O software deve respeitar:
- hardware do usuário;
- concorrência de acesso ao SQLite;
- recuperação após falha;
- crescimento gradual de funcionalidades;
- futura migração para backend web/API.

---

## 2. Princípios de engenharia de sistemas

### 2.1 Requisitos funcionais mínimos
O produto deve sustentar:
- cadastros;
- lançamentos;
- indicadores de caixa;
- visão de margem e custo;
- relatórios operacionais;
- exportação simples;
- histórico confiável.

### 2.2 Requisitos não funcionais mínimos
- inicialização rápida;
- baixo consumo de recursos;
- persistência confiável;
- mensagens de erro claras;
- logs mínimos;
- UX simples;
- backup fácil do banco.

### 2.3 Arquitetura guiada por evolução
Construir desde já pensando em:
- API futura;
- autenticação futura;
- multiempresa futura;
- multiusuário futuro;
- sincronização futura.

### 2.4 Riscos que precisam ser controlados
- lógica de negócio acoplada à UI;
- campos livres demais e sem validação;
- datas e valores monetários sem regra;
- fórmula financeira espalhada pelo código;
- refatoração sem testes;
- banco local sem estratégia de integridade.

---

## 3. Boas práticas obrigatórias

### 3.1 Código
- usar `nullable` habilitado;
- tratar warnings como erros;
- evitar métodos gigantes;
- evitar classes “Deus”;
- usar nomes claros;
- não misturar cálculo financeiro com componentes visuais.

### 3.2 Modelagem
- usar entidades pequenas e expressivas;
- preferir tipos explícitos;
- manter validação de invariantes no domínio;
- centralizar fórmulas financeiras na camada certa.

### 3.3 Banco
- usar SQLite como começo;
- manter configurações EF em classes separadas;
- proteger precisão de valores monetários;
- nunca depender de string solta para datas e valores.

### 3.4 Refatoração
- refatorar por etapas pequenas;
- preservar comportamento antes de otimizar;
- mover regra de negócio para o domínio/application;
- criar testes antes de mexer em fórmulas críticas.

### 3.5 Deploy e operação
- preparar `appsettings.json`;
- manter banco em local previsível;
- padronizar logs;
- documentar atualização e backup.

---

## 4. O que um sênior engenheiro faria

1. Definiria o **bounded context** inicial e evitaria escopo excessivo.
2. Começaria pelo **núcleo do domínio** e não pela interface.
3. Criaria uma solução pequena, mas com limites claros entre camadas.
4. Escolheria métricas iniciais de sucesso:
   - tempo para cadastrar;
   - tempo para lançar despesas/receitas;
   - clareza do dashboard;
   - confiabilidade do fechamento mensal.
5. Implementaria primeiro o “fluxo mínimo que entrega valor”.
6. Escreveria testes nos pontos em que o erro custa caro.
7. Só depois ampliaria a UX e o visual.

---

## 5. O que um sênior arquiteto faria

1. Garantiria que o desktop-first não bloqueie o SaaS depois.
2. Isolaria o domínio para poder reaproveitar no backend futuro.
3. Criaria interfaces estáveis na camada Application.
4. Evitaria dependências desnecessárias.
5. Pensaria em:
   - persistência;
   - versionamento de dados;
   - migração futura;
   - segurança operacional;
   - suporte e manutenção.

---

## 6. Passo a passo de execução no Antigravity

### Sprint 0 — base
- abrir a solução;
- restaurar pacotes;
- validar build;
- ajustar tema, fonte e UX mínima.

### Sprint 1 — domínio e persistência
- validar entidades;
- criar migrations;
- revisar precisão monetária;
- garantir seed local.

### Sprint 2 — dashboard + lançamentos
- implementar leitura do resumo;
- criar listagem e cadastro de lançamentos;
- validar filtros de período.

### Sprint 3 — clientes e produtos/serviços
- CRUD completo;
- pesquisa;
- validações de formulário;
- histórico simples.

### Sprint 4 — indicadores gerenciais
- margem de contribuição;
- ponto de equilíbrio;
- margem de segurança;
- fluxo de caixa por período.

### Sprint 5 — endurecimento do sistema
- testes;
- logs;
- backup/restore;
- exportação CSV/PDF;
- revisão de UX.

### Sprint 6 — trilha SaaS
- extrair API;
- autenticação;
- multiempresa;
- sincronização e deploy.

---

## 7. Definição de pronto

Uma funcionalidade só está pronta quando:
- compila;
- funciona;
- valida entradas;
- respeita a arquitetura;
- tem pelo menos um teste quando a regra é crítica;
- está documentada se alterar fluxo relevante.

---

## 8. Prompt mestre para uso com Claude/Codex

```text
Você é um engenheiro de software sênior e arquiteto de sistemas responsável por evoluir a solução SMEFinanceSuite em C# no Ubuntu 25.10, usando Antigravity como IDE.

Regras obrigatórias:
1. Respeite a arquitetura existente: Domain, Application, Infrastructure, Desktop e Bootstrapper.
2. Não misture regra de negócio com interface.
3. Mantenha código simples, limpo, tipado e com baixo acoplamento.
4. Preserve compatibilidade futura com API/SaaS.
5. Antes de alterar fórmulas críticas, escreva ou atualize testes.
6. Antes de refatorar, explique rapidamente o motivo e o impacto.
7. Nunca duplique regra de negócio. Extraia para o lugar correto.
8. Mantenha nomes claros e módulos pequenos.
9. Gere código pronto para produção inicial, não apenas protótipo.
10. Ao final de cada etapa, entregue:
   - o que foi criado;
   - o que foi alterado;
   - próximos passos;
   - riscos ou pendências.

Prioridade inicial:
- consolidar CRUD de lançamentos;
- consolidar dashboard;
- implementar clientes e produtos/serviços;
- adicionar indicadores financeiros;
- preparar a solução para futura API.
```
