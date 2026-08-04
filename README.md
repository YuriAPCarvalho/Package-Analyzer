# Package-Analyzer by: YuriAPCarvalho

Aplicação desktop local para cadastrar projetos, executar scans do Trivy e visualizar histórico de vulnerabilidades sem enviar código, caminhos ou relatórios para serviços externos.

## Arquitetura

A solução usa .NET 9, Avalonia UI, MVVM, CommunityToolkit.Mvvm, SQLite, EF Core, System.Text.Json e `System.Diagnostics.Process`.

```text
src/
  TrivyProjectManager.App/             Avalonia UI, ViewModels, DI e navegação
  TrivyProjectManager.Application/     Contratos, DTOs, parser e regras de aplicação
  TrivyProjectManager.Domain/          Entidades, enums e chaves lógicas
  TrivyProjectManager.Infrastructure/  SQLite, processos, Trivy, storage e retenção
tests/
  TrivyProjectManager.UnitTests/
  TrivyProjectManager.IntegrationTests/
samples/trivy-reports/
```

## Requisitos

- .NET SDK com suporte a `net9.0`.
- Trivy instalado localmente.
- Windows é o alvo inicial. A arquitetura evita APIs Windows fora de seleção de arquivo/janela e abertura explícita de links.

## Instalar o Trivy

No Windows, uma opção é instalar via Chocolatey:

```powershell
choco install trivy
```

Ou baixar o executável em https://github.com/aquasecurity/trivy/releases e configurar o caminho do `trivy.exe` na tela de configurações. A aplicação tenta detectar o Trivy pelo `PATH`.

## Executar

```powershell
dotnet restore TrivyProjectManager.slnx
dotnet build TrivyProjectManager.slnx
dotnet run --project src\TrivyProjectManager.App\TrivyProjectManager.App.csproj
```

Se o restore/build ficar preso ou falhar sem erro neste ambiente, use:

```powershell
$env:MSBUILDDISABLENODEREUSE = '1'
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.slnx -m:1 -nr:false
```

## Dados Locais

- Banco SQLite: `%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db`
- Configurações: `%LOCALAPPDATA%\TrivyProjectManager\settings.json`
- Relatórios centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\`
- Logs centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\`
- Armazenamento no projeto, quando habilitado: `.security/trivy/`

A aplicação nunca altera `.gitignore` automaticamente. Use o botão dedicado nas configurações do projeto.

## Uso

1. Clique em `Adicionar projeto`.
2. Selecione uma pasta local.
3. A aplicação detecta .NET, NPM, pnpm ou Yarn por arquivos como `.sln`, `.csproj`, `package.json`, `package-lock.json`, `pnpm-lock.yaml` e `yarn.lock`.
4. Ajuste tecnologia, package manager, armazenamento e comandos na aba `Configurações`.
5. Execute `Scan rápido` para rodar somente o Trivy.
6. Execute `Scan completo` para rodar comandos habilitados e depois o Trivy.

O scan completo mostra aviso porque restore/install/build/test podem executar scripts definidos pelo projeto.

## Privacidade

O Package-Analyzer by: YuriAPCarvalho foi desenvolvido para funcionar de forma local-first. Não há login, telemetria, analytics, upload automático, integração em nuvem ou enriquecimento externo de CVEs nesta versão.

A aplicação não envia código-fonte, relatórios, nomes de projetos, caminhos locais ou resultados de scans para serviços externos. Os scans são executados pelo Trivy instalado localmente.

O Trivy pode acessar a internet para baixar ou atualizar sua base pública de vulnerabilidades. Possíveis segredos são mascarados antes de aparecer na interface ou serem persistidos, e links de referência só são abertos por ação explícita do usuário.

## Testes

```powershell
dotnet test TrivyProjectManager.slnx
```

Neste ambiente, os comandos validados foram:

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.sln --no-restore -m:1 -nr:false
dotnet test TrivyProjectManager.sln --no-restore -m:1 -nr:false
```

Os testes cobrem detecção de projeto, parser do JSON do Trivy, deduplicação, contadores, comparação entre scans, mascaramento de secrets, chave lógica, validação de comandos, migration SQLite, retenção e textos de apresentação em pt-BR.

## Limitações Atuais

- A UI é um MVP funcional de janela única, não um wizard multi-passo completo.
- A comparação classifica findings do scan atual e conta resolvidos contra o scan anterior, mas não cria registros persistidos separados para findings resolvidos.
- A geração de SBOM, scan de imagem Docker, exportações HTML/CSV/SARIF, YAML Azure DevOps e enriquecimento NVD/OSV/GitHub estão preparados apenas por estrutura/interfaces.
- Yarn é detectado por `yarn.lock`, mas comandos Yarn falharão amigavelmente se o executável não estiver instalado.

## Roadmap

- Wizard completo de cadastro com edição antes do primeiro save.
- Visualização de scan antigo como contexto independente.
- Exceções de segurança com validade e fluxo de revisão.
- Exportações HTML, CSV e SARIF.
- SBOM e scan de imagem Docker.
- Enriquecimento opcional e explicitamente configurado.
