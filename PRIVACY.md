# Politica de Privacidade

O Package-Analyzer foi desenvolvido com uma abordagem local-first.

A aplicacao nao envia automaticamente codigo-fonte, relatorios, nomes de projetos, caminhos locais ou resultados de analises para servicos externos.

Os dados da aplicacao, incluindo configuracoes, historico e relatorios, permanecem armazenados no computador do usuario.

Por padrao, os dados locais ficam em:

- `%LOCALAPPDATA%\TrivyProjectManager\data\trivy-project-manager.db`
- `%LOCALAPPDATA%\TrivyProjectManager\settings.json`
- `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\reports\`
- `%LOCALAPPDATA%\TrivyProjectManager\Projects\<project-id>\logs\`

Quando o usuario habilita armazenamento dentro do projeto, relatorios e logs podem ser gravados em `.security/trivy/`.

## Acesso a internet

A aplicacao pode acessar a internet para:

- verificar e baixar atualizacoes oficiais do Package-Analyzer;
- obter ou atualizar o Trivy gerenciado localmente;
- permitir que o Trivy atualize bases publicas de vulnerabilidades;
- abrir referencias externas quando o usuario solicitar explicitamente.

O enriquecimento externo por NVD, OSV ou GitHub Advisory e opcional e vem desativado por padrao. Quando o usuario habilita esse recurso, o Package-Analyzer consulta somente identificadores publicos de vulnerabilidade, como CVE ou GHSA. Codigo-fonte, caminhos locais, relatorios completos e nomes de projetos nao sao enviados durante essas consultas.

O token GitHub Advisory, quando informado pelo usuario, e armazenado localmente em `%LOCALAPPDATA%\TrivyProjectManager\settings.json` e nunca deve ser versionado no repositorio.

## Segredos

Possiveis segredos encontrados nas analises sao exibidos e armazenados de forma mascarada sempre que processados pelo Package-Analyzer.

## Telemetria

O Package-Analyzer nao possui login, telemetria, analytics ou upload automatico de dados.
