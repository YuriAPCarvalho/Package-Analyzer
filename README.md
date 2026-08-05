# Package-Analyzer

**Autor:** YuriAPCarvalho

**SPDX-License-Identifier:** MIT

[![Release](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml/badge.svg)](https://github.com/YuriAPCarvalho/Package-Analyzer/actions/workflows/release.yml)
[![GitHub release](https://img.shields.io/github/v/release/YuriAPCarvalho/Package-Analyzer)](https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest)
[![Licença: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows-0078d4.svg)](#instalação)
[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4.svg)](#desenvolvimento)
[![Código Aberto](https://img.shields.io/badge/status-open%20source-brightgreen.svg)](CONTRIBUTING.md)

O **Package-Analyzer** é uma aplicação desktop desenvolvida para análise local de segurança em projetos de software. A ferramenta automatiza a identificação de vulnerabilidades conhecidas, configurações inseguras e segredos expostos, além de organizar os resultados das análises para facilitar a correção dos problemas encontrados.

O aplicativo utiliza o **Trivy** como um de seus mecanismos de análise. O Trivy é um projeto de código aberto mantido pela **Aqua Security**.

> **Aviso:** O Package-Analyzer é um projeto independente de código aberto e não possui qualquer vínculo, parceria ou endosso da Aqua Security.

---

# Funcionalidades

- Cadastro e gerenciamento de projetos locais.
- Detecção automática de projetos .NET, NPM, pnpm e Yarn.
- Análise rápida utilizando o Trivy instalado localmente.
- Análise completa com execução opcional de restauração de dependências, instalação, compilação e testes antes da verificação.
- Painel de indicadores contendo:
  - Vulnerabilidades por severidade.
  - Vulnerabilidades únicas.
  - Configurações inseguras (_Misconfigurations_).
  - Segredos identificados (_Secrets_).

- Detalhamento de vulnerabilidades (CVE e GHSA), incluindo:
  - Pacote afetado.
  - Versão instalada.
  - Versão corrigida.
  - Referências oficiais.

- Agrupamento automático de ocorrências equivalentes.
- Histórico de análises com comparação entre execuções.
- Identificação de vulnerabilidades:
  - Novas.
  - Existentes.
  - Regressões.
  - Resolvidas.

- Abas específicas para configurações inseguras e segredos.
- Ocultação automática de segredos antes da exibição e do armazenamento.
- Banco de dados SQLite armazenado localmente.
- Atualizações automáticas utilizando Velopack.
- Instalador oficial para Windows x64.

---

# Capturas de tela

As imagens públicas do aplicativo devem ser armazenadas em:

```text
docs/images/
```

Quando novas capturas de tela forem adicionadas, substitua esta observação pelas imagens correspondentes.

---

# Instalação

O Package-Analyzer é distribuído inicialmente para **Windows x64**.

## Passo a passo

1. Acesse a página oficial de versões:

   https://github.com/YuriAPCarvalho/Package-Analyzer/releases/latest

2. Baixe o arquivo:

```text
YuriAPCarvalho.PackageAnalyzer-stable-Setup.exe
```

3. Verifique o hash SHA-256 disponível no arquivo `SHA256SUMS.txt`.

4. Execute o instalador.

---

# Aviso de segurança

O aplicativo ainda **não possui assinatura digital**.

Durante a instalação ou na primeira execução, o Windows SmartScreen poderá exibir um aviso de segurança.

Para garantir a autenticidade do arquivo:

- Faça o download somente pelo repositório oficial do GitHub.
- Verifique o hash SHA-256 publicado juntamente com cada versão.

---

# Dados armazenados localmente

Banco de dados SQLite

```text
%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db
```

Configurações

```text
%LOCALAPPDATA%\TrivyProjectManager\settings.json
```

Relatórios

```text
%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\
```

Logs

```text
%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\
```

Armazenamento dentro do projeto (opcional)

```text
.security/trivy/
```

A desinstalação remove apenas o aplicativo. Os dados armazenados em `%LOCALAPPDATA%\TrivyProjectManager` podem permanecer no computador para preservar o histórico das análises e as configurações do usuário.

---

# Privacidade

O Package-Analyzer:

- Não exige cadastro.
- Não exige autenticação.
- Não coleta telemetria.
- Não envia automaticamente código-fonte.
- Não envia nomes de projetos.
- Não envia caminhos locais.
- Não envia relatórios.
- Não envia resultados das análises.

O acesso à internet ocorre somente quando necessário para:

- Verificar novas versões do Package-Analyzer.
- Baixar ou atualizar o Trivy gerenciado pelo aplicativo.
- Atualizar a base pública de vulnerabilidades do Trivy.
- Abrir referências externas solicitadas pelo usuário.

O enriquecimento das informações por meio da NVD, OSV ou GitHub Advisory é opcional e permanece desativado por padrão.

Quando habilitado, apenas identificadores públicos, como **CVE** e **GHSA**, são consultados. Nenhum conteúdo do projeto analisado é enviado.

Mais informações em:

- [PRIVACY.md](PRIVACY.md)

---

# Desenvolvimento

## Requisitos

- .NET SDK 9.0
- Windows
- Trivy instalado localmente ou instalação automática habilitada

## Comandos principais

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false

dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false

dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false

dotnet run --project src\TrivyProjectManager.App\TrivyProjectManager.App.csproj
```

## Estrutura do projeto

```text
src/
├── TrivyProjectManager.App/
├── TrivyProjectManager.Application/
├── TrivyProjectManager.Domain/
└── TrivyProjectManager.Infrastructure/

tests/
├── TrivyProjectManager.UnitTests/
└── TrivyProjectManager.IntegrationTests/

samples/
└── trivy-reports/
```

O aplicativo procura automaticamente pelo executável `trivy.exe` no `PATH` do sistema ou no caminho configurado pelo usuário.

Quando a instalação automática estiver habilitada, o Trivy será baixado para:

```text
%LOCALAPPDATA%\TrivyProjectManager\tools\trivy\trivy.exe
```

---

# Publicação de versões

As versões oficiais são disponibilizadas em:

https://github.com/YuriAPCarvalho/Package-Analyzer/releases

Para criar uma nova versão:

```powershell
$tag = "v0.1.1"

git tag $tag

git push origin $tag
```

Cada versão deve seguir o padrão:

```text
vMAJOR.MINOR.PATCH
```

Também deve possuir uma entrada correspondente no arquivo `CHANGELOG.md`.

Exemplo:

```md
## [0.1.1]
```

As etiquetas (**tags**) de versão são imutáveis e nunca devem ser reutilizadas.

A etiqueta `v0.1.0` foi preservada apenas como registro histórico de uma tentativa de publicação anterior.

O fluxo automatizado localizado em:

```text
.github/workflows/release.yml
```

é responsável por:

- Restaurar as dependências.
- Compilar a aplicação.
- Executar os testes.
- Publicar a versão final.
- Empacotar utilizando Velopack.
- Gerar o arquivo `SHA256SUMS.txt`.
- Publicar automaticamente os artefatos na página de versões do GitHub.

---

# Assinatura de código

A integração com o **SignPath Foundation** está prevista, mas ainda aguarda aprovação.

Mais informações em:

- [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md)

---

# Documentação

- [Política de Privacidade](PRIVACY.md)
- [Política de Segurança](SECURITY.md)
- [Guia de Contribuição](CONTRIBUTING.md)
- [Código de Conduta](CODE_OF_CONDUCT.md)
- [Política de Assinatura de Código](CODE_SIGNING_POLICY.md)
- [Avisos sobre Componentes de Terceiros](THIRD_PARTY_NOTICES.md)
- [Licença](LICENSE)
- [Lista de Verificação do SignPath](docs/SIGNPATH_APPLICATION_CHECKLIST.md)
- [Relatório de Preparação para Código Aberto](docs/OPEN_SOURCE_PREPARATION_REPORT.md)
