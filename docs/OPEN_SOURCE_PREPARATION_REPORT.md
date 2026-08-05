# Open source preparation report

## Implementado

- Repositorio principal preparado para concentrar codigo, documentacao, releases, checksums e artefatos Velopack.
- Dependencia do repositorio `Package-Analyzer-Download` removida do app, README e workflow.
- Licenca MIT, politicas, templates, CODEOWNERS, checklist SignPath e avisos de terceiros adicionados.
- Workflow de release atualizado para publicar no proprio repositorio usando `GITHUB_TOKEN`.

## Seguranca

- Busca estatica no estado atual nao encontrou arquivos versionados sensiveis como `.env`, bancos SQLite, logs, certificados ou chaves.
- Busca estatica no estado atual e no historico nao encontrou PATs GitHub, chaves privadas ou cabecalhos `Authorization: Bearer` com valor literal.
- A busca ampla por palavras como `token`, `password`, `api_key` e `secret` encontrou apenas referencias esperadas em documentacao, testes, codigo de mascaramento, configuracao local opt-in do GitHub Advisory e fixtures sinteticas.
- O historico contem referencias antigas ao nome do secret do workflow (`PUBLIC_RELEASE_TOKEN`) e ao repositorio `Package-Analyzer-Download`, sem valor de token literal.
- Gitleaks nao estava instalado neste ambiente; validacao final ainda deve ser feita com:

```powershell
gitleaks detect --source . --redact --verbose
```

- A fixture de secret em `samples/trivy-reports/secret.json` usa valor fake e existe apenas para testar mascaramento.
- Nenhum valor de secret real deve ser documentado ou exibido em logs.

## Open Source

- Licenca: MIT.
- Autoria preservada: `Package-Analyzer by: YuriAPCarvalho`.
- Politicas adicionadas: privacidade, seguranca, contribuicao, conduta e assinatura.
- Terceiros documentados em `THIRD_PARTY_NOTICES.md`.

## Release

- Tags aceitas: `vMAJOR.MINOR.PATCH`.
- Cada tag exige secao `## [MAJOR.MINOR.PATCH]` no `CHANGELOG.md`.
- O workflow publica os artefatos no GitHub Release do proprio repositorio.
- Checksums SHA-256 sao publicados em `SHA256SUMS.txt`.
- Nenhum `PUBLIC_RELEASE_TOKEN` e necessario no modelo de repo unico.

## SignPath

- Integracao SignPath Foundation: planned / application pending.
- Pendentes manuais: tornar repositorio publico, ativar 2FA, publicar release publica, enviar formulario, configurar integracao apos aprovacao e aprovar manualmente cada assinatura.
- Limitacao: enquanto a aprovacao nao existir, os binarios seguem sem assinatura digital.

## Comandos validados

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
```

Resultado: sucesso.

```powershell
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
```

Resultado: sucesso, 0 avisos, 0 erros.

```powershell
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
```

Resultado: sucesso, 32 testes aprovados.

```powershell
gitleaks detect --source . --redact --verbose
```

Resultado: Gitleaks nao instalado neste ambiente.

```powershell
git ls-files | rg -i "(^|/)(\.env|appsettings.*\.json)$|\.(pfx|p12|cer|key|pem|sqlite|sqlite3|db|db-shm|db-wal|log|dmp|dump)$|(^|/)(logs|reports|TestResults|\.security/trivy)(/|$)|Users/|C:/Users|C:\\Users"
```

Resultado: nenhum arquivo versionado sensivel encontrado.

```powershell
rg -n -i "github_pat_|ghp_|gho_|ghs_|ghu_|BEGIN PRIVATE KEY|Authorization:\s*Bearer" .
git grep -n -I -E "github_pat_|ghp_|gho_|ghs_|ghu_|BEGIN PRIVATE KEY|Authorization:[[:space:]]*Bearer" $(git rev-list --all)
```

Resultado: nenhum match de PAT GitHub, chave privada ou bearer token literal.

## Pendencias manuais

- Tornar o repositorio publico.
- Revisar se todos os arquivos podem ser publicados.
- Ativar 2FA.
- Conferir deteccao da licenca MIT no GitHub.
- Publicar uma release publica.
- Excluir manualmente o repositorio `Package-Analyzer-Download`, se ainda desejar.
- Enviar a solicitacao a SignPath Foundation.
- Configurar a integracao SignPath apos aprovacao.
- Aprovar manualmente cada solicitacao de assinatura.
