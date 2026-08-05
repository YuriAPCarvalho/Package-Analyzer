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
- Trivy instalado localmente ou instalação automática habilitada no app.
- Windows é o alvo inicial. A arquitetura evita APIs Windows fora de seleção de arquivo/janela e abertura explícita de links.

## Instalar o Trivy

No Windows, uma opção é instalar via Chocolatey:

```powershell
choco install trivy
```

Ou baixar o executável em https://github.com/aquasecurity/trivy/releases e configurar o caminho do `trivy.exe` na tela de configurações. A aplicação tenta detectar o Trivy pelo `PATH`.

Por padrão, o Package-Analyzer também consegue baixar o Trivy automaticamente na primeira execução. A cópia gerenciada fica em `%LOCALAPPDATA%\TrivyProjectManager\tools\trivy\trivy.exe` e pode ser atualizada ao abrir o app quando a opção `Verificar atualização do Trivy ao abrir` estiver habilitada.

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

A aplicação não envia código-fonte, relatórios, nomes de projetos, caminhos locais ou resultados de scans para serviços externos. Os scans são executados pelo Trivy local.

Quando a instalação/atualização automática estiver habilitada, a aplicação pode acessar os releases públicos do Trivy no GitHub/Aqua Security para baixar ou atualizar o `trivy.exe` gerenciado. O Trivy também pode acessar a internet para baixar ou atualizar sua base pública de vulnerabilidades. Possíveis segredos são mascarados antes de aparecer na interface ou serem persistidos, e links de referência só são abertos por ação explícita do usuário.

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

## Publicação e Atualizações

A distribuição Windows x64 usa Velopack e GitHub Releases públicos em `https://github.com/YuriAPCarvalho/Package-Analyzer-Download`.

Este repositório contém o código-fonte privado do Package-Analyzer. O repositório público `Package-Analyzer-Download` deve conter apenas documentação pública e artefatos de release para download, sem `src/`, `tests/`, migrations, solution files ou samples internos.

Os assets de marca ficam em `src\TrivyProjectManager.App\Assets`: `app-icon.ico` é usado pelo executável, janelas, taskbar e atalhos do Windows; `app-logo.png` é usado no splash da instalação e na interface.

Para publicar uma nova versão:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

O workflow `.github/workflows/release.yml` restaura, compila, testa, publica o app self-contained, empacota com `vpk pack` e envia os artefatos para o GitHub Release público. Cada tag `vMAJOR.MINOR.PATCH` precisa ter uma seção correspondente no `CHANGELOG.md`, por exemplo `## [0.1.0]`.

Para publicar no repositório público, configure no repositório privado o secret `PUBLIC_RELEASE_TOKEN` com um fine-grained PAT com acesso ao repositório `Package-Analyzer-Download` e permissão `Contents: Read and write`.

A assinatura digital ainda não está ativa. Quando houver certificado ou Azure Trusted Signing configurado, adicione `--signParams` ou `--azureTrustedSignFile` no passo `Pack Velopack release` do workflow.

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
