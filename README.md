# Package-Analyzer

by: YuriAPCarvalho

SPDX-License-Identifier: MIT

[![Release](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml/badge.svg)](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/YuriAPCarvalho/Package-Analyzer)](https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows-0078d4.svg)](#instalacao)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4.svg)](#desenvolvimento)
[![Open Source](https://img.shields.io/badge/status-open%20source-brightgreen.svg)](CONTRIBUTING.md)

O Package-Analyzer e uma aplicacao desktop local-first para preparar projetos, executar analises de seguranca, organizar vulnerabilidades, acompanhar o historico de scans e apresentar correcoes de forma clara.

Trivy is an open-source project maintained by Aqua Security and is used as one of the local analysis engines supported by Package-Analyzer.

Package-Analyzer is an independent open-source project and is not affiliated with or endorsed by Aqua Security.

## Funcionalidades

- Cadastro de projetos locais.
- Deteccao de projetos .NET, NPM, pnpm e Yarn.
- Scan rapido com Trivy local.
- Scan completo com restore/install/build/test configuraveis antes da analise.
- Dashboard por severidade, vulnerabilidades unicas, misconfigurations e secrets.
- Detalhes de CVE/GHSA, pacote afetado, versao instalada, versao corrigida e referencias HTTPS.
- Agrupamento de ocorrencias equivalentes.
- Historico de scans, comparacao com scan anterior e classificacao de novos, existentes, regressao e resolvidos.
- Misconfigurations e secrets em abas dedicadas.
- Mascaramento de secrets antes da exibicao e persistencia.
- Banco SQLite e configuracoes armazenados localmente.
- Atualizacoes automaticas via Velopack quando instalado pelo instalador oficial.
- Instalador Windows x64 gerado pelo workflow de release.

## Screenshots

As imagens publicas do aplicativo devem ficar em `docs/images/`.

Quando novas screenshots forem adicionadas, remova esta nota e referencie os arquivos versionados aqui.

## Instalacao

O alvo inicial e Windows x64.

1. Abra a pagina oficial de releases: https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest
2. Baixe `YuriAPCarvalho.PackageAnalyzer-stable-Setup.exe`.
3. Confira o hash SHA-256 no arquivo `SHA256SUMS.txt` publicado na mesma release.
4. Execute o instalador.

## Aviso de seguranca

O aplicativo ainda nao possui assinatura digital.

Por esse motivo, o Windows SmartScreen pode exibir um alerta durante a instalacao ou na primeira execucao.

Baixe o aplicativo somente por meio do repositorio oficial e confirme que o arquivo foi obtido pela pagina de Releases.

Confira tambem o checksum SHA-256 publicado junto ao instalador.

## Dados locais

- Banco SQLite: `%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db`
- Configuracoes: `%LOCALAPPDATA%\TrivyProjectManager\settings.json`
- Relatorios centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\`
- Logs centrais: `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\`
- Armazenamento no projeto, quando habilitado: `.security/trivy/`

A desinstalacao do aplicativo remove a instalacao, mas os dados locais podem permanecer em `%LOCALAPPDATA%\TrivyProjectManager` para preservar historico e configuracoes.

## Privacidade

O Package-Analyzer nao exige login, nao possui telemetria e nao envia automaticamente codigo-fonte, nomes de projetos, caminhos locais, relatorios ou resultados de scans para servicos externos.

A aplicacao pode acessar a internet para verificar e baixar atualizacoes oficiais do Package-Analyzer, baixar ou atualizar o Trivy gerenciado localmente, permitir que o Trivy atualize bases publicas de vulnerabilidades e abrir referencias externas quando o usuario solicitar.

O enriquecimento externo por NVD, OSV ou GitHub Advisory e opt-in e vem desativado por padrao. Quando habilitado, apenas identificadores publicos como CVE ou GHSA sao consultados; o conteudo dos projetos analisados nao e enviado.

Veja [PRIVACY.md](PRIVACY.md).

## Desenvolvimento

Requisitos:

- .NET SDK 9.0.
- Windows para validar instalador, atalhos e experiencia desktop alvo.
- Trivy instalado localmente ou instalacao automatica habilitada no app.

Comandos principais:

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
dotnet run --project src\TrivyProjectManager.App\TrivyProjectManager.App.csproj
```

Estrutura:

```text
src/
  TrivyProjectManager.App/             Avalonia UI, ViewModels, DI e Velopack
  TrivyProjectManager.Application/     Contratos, DTOs, parser e regras de aplicacao
  TrivyProjectManager.Domain/          Entidades, enums e chaves logicas
  TrivyProjectManager.Infrastructure/  SQLite, processos, Trivy, storage e retencao
tests/
  TrivyProjectManager.UnitTests/
  TrivyProjectManager.IntegrationTests/
samples/trivy-reports/
```

O app tenta localizar `trivy.exe` pelo `PATH` ou pelo caminho configurado. Se a instalacao automatica estiver habilitada, baixa a release Windows x64 do Trivy em `%LOCALAPPDATA%\TrivyProjectManager\tools\trivy\trivy.exe`.

## Releases

As releases oficiais ficam neste repositorio: https://github.com/YuriAPCarvalho/Package-Analyzer/releases

Para criar uma nova versao:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

Cada tag deve usar `vMAJOR.MINOR.PATCH` e ter uma secao correspondente no `CHANGELOG.md`, por exemplo `## [0.1.0]`.

O workflow `.github/workflows/release.yml` restaura, compila, testa, publica o app self-contained, empacota com Velopack, gera `SHA256SUMS.txt` e publica os artefatos no GitHub Release do proprio repositorio.

## Code signing policy

SignPath Foundation integration: planned / application pending.

Veja [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

## Documentacao

- [Privacy policy](PRIVACY.md)
- [Security policy](SECURITY.md)
- [Contributing](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Code signing policy](CODE_SIGNING_POLICY.md)
- [Third-party notices](THIRD_PARTY_NOTICES.md)
- [License](LICENSE)
- [SignPath checklist](docs/SIGNPATH_APPLICATION_CHECKLIST.md)
- [Open source preparation report](docs/OPEN_SOURCE_PREPARATION_REPORT.md)
