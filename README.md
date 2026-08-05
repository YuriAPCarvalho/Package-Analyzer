# Package-Analyzer

by: YuriAPCarvalho

SPDX-License-Identifier: MIT

[![Release](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml/badge.svg)](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/YuriAPCarvalho/Package-Analyzer)](https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows-0078d4.svg)](#instalação)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4.svg)](#desenvolvimento)
[![Open Source](https://img.shields.io/badge/status-open%20source-brightgreen.svg)](CONTRIBUTING.md)

O Package-Analyzer é uma aplicação desktop **local-first** para preparar projetos, executar análises de segurança, organizar vulnerabilidades, acompanhar o histórico de scans e apresentar correções de forma clara.

Trivy is an open-source project maintained by Aqua Security and is used as one of the local analysis engines supported by Package-Analyzer.

Package-Analyzer is an independent open-source project and is not affiliated with or endorsed by Aqua Security.

## Funcionalidades

- Cadastro de projetos locais.
- Detecção de projetos .NET, NPM, pnpm e Yarn.
- Scan rápido com Trivy local.
- Scan completo com restore/install/build/test configuráveis antes da análise.
- Dashboard por severidade, vulnerabilidades únicas, misconfigurations e secrets.
- Detalhes de CVE/GHSA, pacote afetado, versão instalada, versão corrigida e referências HTTPS.
- Agrupamento de ocorrências equivalentes.
- Histórico de scans, comparação com o scan anterior e classificação de novos, existentes, regressões e resolvidos.
- Misconfigurations e secrets em abas dedicadas.
- Mascaramento de secrets antes da exibição e persistência.
- Banco SQLite e configurações armazenados localmente.
- Atualizações automáticas via Velopack quando instalado pelo instalador oficial.
- Instalador Windows x64 gerado pelo workflow de release.

## Screenshots

As imagens públicas do aplicativo devem ficar em `docs/images/`.

Quando novas screenshots forem adicionadas, remova esta nota e referencie os arquivos versionados nesta seção.

## Instalação

O alvo inicial é o Windows x64.

1. Abra a página oficial de Releases: https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest
2. Baixe `YuriAPCarvalho.PackageAnalyzer-stable-Setup.exe`.
3. Confira o hash SHA-256 no arquivo `SHA256SUMS.txt` publicado na mesma release.
4. Execute o instalador.

## Aviso de segurança

O aplicativo ainda não possui assinatura digital.

Por esse motivo, o Windows SmartScreen pode exibir um alerta durante a instalação ou na primeira execução.

Baixe o aplicativo somente por meio do repositório oficial e confirme que o arquivo foi obtido pela página de Releases.

Confira também o checksum SHA-256 publicado junto ao instalador.

## Dados locais

- Banco SQLite: `%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db`
- Configurações: `%LOCALAPPDATA%\TrivyProjectManager\settings.json`
- Relatórios centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\`
- Logs centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\`
- Armazenamento no projeto, quando habilitado: `.security/trivy/`

A desinstalação do aplicativo remove a instalação, mas os dados locais podem permanecer em `%LOCALAPPDATA%\TrivyProjectManager` para preservar o histórico e as configurações.

## Privacidade

O Package-Analyzer não exige login, não possui telemetria e não envia automaticamente código-fonte, nomes de projetos, caminhos locais, relatórios ou resultados de scans para serviços externos.

A aplicação pode acessar a internet para:

- Verificar e baixar atualizações oficiais do Package-Analyzer.
- Baixar ou atualizar o Trivy gerenciado localmente.
- Permitir que o Trivy atualize as bases públicas de vulnerabilidades.
- Abrir referências externas quando solicitado pelo usuário.

O enriquecimento externo por NVD, OSV ou GitHub Advisory é **opt-in** e vem desativado por padrão. Quando habilitado, apenas identificadores públicos, como CVE ou GHSA, são consultados; o conteúdo dos projetos analisados não é enviado.

Veja [PRIVACY.md](PRIVACY.md).

## Desenvolvimento

### Requisitos

- .NET SDK 9.0.
- Windows para validar o instalador, atalhos e a experiência desktop.
- Trivy instalado localmente ou instalação automática habilitada no aplicativo.

### Comandos principais

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
dotnet run --project src\TrivyProjectManager.App\TrivyProjectManager.App.csproj
```

### Estrutura

```text
src/
  TrivyProjectManager.App/             Avalonia UI, ViewModels, DI e Velopack
  TrivyProjectManager.Application/     Contratos, DTOs, parser e regras de aplicação
  TrivyProjectManager.Domain/          Entidades, enums e chaves lógicas
  TrivyProjectManager.Infrastructure/  SQLite, processos, Trivy, storage e retenção
tests/
  TrivyProjectManager.UnitTests/
  TrivyProjectManager.IntegrationTests/
samples/trivy-reports/
```

O aplicativo tenta localizar `trivy.exe` pelo `PATH` ou pelo caminho configurado. Se a instalação automática estiver habilitada, baixa a release Windows x64 do Trivy em `%LOCALAPPDATA%\TrivyProjectManager\tools\trivy\trivy.exe`.

## Releases

As releases oficiais ficam neste repositório:

https://github.com/YuriAPCarvalho/Package-Analyzer/releases

Para criar uma nova versão:

```powershell
$tag = "v0.1.1"
git tag $tag
git push origin $tag
```

Cada tag deve seguir o padrão `vMAJOR.MINOR.PATCH` e possuir uma seção correspondente no `CHANGELOG.md`, por exemplo:

```md
## [0.1.1]
```

Tags de release são imutáveis: nunca mova ou reutilize uma tag já publicada. A tag `v0.1.0` foi preservada como registro de uma tentativa de publicação que falhou antes da migração para o modelo de repositório único. A primeira release válida planejada é `v0.1.1`, e sua tag só deve ser criada com autorização explícita do mantenedor.

O workflow `.github/workflows/release.yml` restaura, compila, testa, publica o aplicativo self-contained, empacota com Velopack, gera o arquivo `SHA256SUMS.txt` e publica os artefatos no GitHub Release deste repositório.

## Code signing policy

SignPath Foundation integration: planned / application pending.

Veja [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

## Documentação

- [Privacy policy](PRIVACY.md)
- [Security policy](SECURITY.md)
- [Contributing](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Code signing policy](CODE_SIGNING_POLICY.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)
- [License](LICENSE)
- [SignPath checklist](docs/SIGNPATH_APPLICATION_CHECKLIST.md)
- [Open source preparation report](docs/OPEN_SOURCE_PREPARATION_REPORT.md)
