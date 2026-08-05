# Open source preparation report

## Implementado

- Repositorio principal preparado para concentrar codigo, documentacao, releases, checksums e artefatos Velopack.
- Dependencia do repositorio `Package-Analyzer-Download` removida do app, README e workflow.
- Licenca MIT, politicas, templates, CODEOWNERS, checklist SignPath e avisos de terceiros adicionados.
- Workflow de release atualizado para publicar no proprio repositorio usando `GITHUB_TOKEN`.
- Repositorio principal tornado publico e licenca MIT detectada pelo GitHub.
- Repositorio `Package-Analyzer-Download` nao encontrado na verificacao final.
- Correcao do YAML de release e CI com actionlint, build e testes preparados localmente; commit, push e execucao do CI ainda pendentes.

## Seguranca

- Busca estatica no estado atual nao encontrou arquivos versionados sensiveis como `.env`, bancos SQLite, logs, certificados ou chaves.
- Busca estatica no estado atual e no historico nao encontrou PATs GitHub, chaves privadas ou cabecalhos `Authorization: Bearer` com valor literal.
- A busca ampla por palavras como `token`, `password`, `api_key` e `secret` encontrou apenas referencias esperadas em documentacao, testes, codigo de mascaramento, configuracao local opt-in do GitHub Advisory e fixtures sinteticas.
- O historico contem referencias antigas ao nome do secret do workflow (`PUBLIC_RELEASE_TOKEN`) e ao repositorio `Package-Analyzer-Download`, sem valor de token literal.
- Gitleaks 8.30.1 foi executado temporariamente, sem instalacao global, com:

```powershell
gitleaks detect --source . --redact --verbose
```

- Resultado: 9 commits verificados e nenhum leak encontrado.
- A verificacao complementar do working tree com `gitleaks dir . --redact --verbose` analisou aproximadamente 6,91 MB e tambem nao encontrou leaks.
- A fixture de secret em `samples/trivy-reports/secret.json` usa valor fake e existe apenas para testar mascaramento.
- Nenhum valor de secret real deve ser documentado ou exibido em logs.

## Open Source

- Licenca: MIT.
- Autoria preservada: `Package-Analyzer by: YuriAPCarvalho`.
- Politicas adicionadas: privacidade, seguranca, contribuicao, conduta e assinatura.
- Terceiros documentados em `THIRD_PARTY_NOTICES.md`.

## Release

- Tags aceitas: `vMAJOR.MINOR.PATCH`.
- Tags publicadas sao imutaveis e nao devem ser movidas ou reutilizadas.
- Cada tag exige secao `## [MAJOR.MINOR.PATCH]` no `CHANGELOG.md`.
- O workflow publica os artefatos no GitHub Release do proprio repositorio.
- Checksums SHA-256 sao publicados em `SHA256SUMS.txt`.
- Nenhum `PUBLIC_RELEASE_TOKEN` e necessario no modelo de repo unico.
- A tag remota `v0.1.0` aponta para `14d075a`; suas execucoes falharam no workflow legado e nenhuma GitHub Release foi publicada.
- A primeira release valida planejada e `v0.1.1`, somente apos commit, push, CI verde e autorizacao explicita para a tag.

## SignPath

- Integracao SignPath Foundation: planned / application pending.
- Pendentes manuais: revisar e publicar as correcoes locais, ativar Private Vulnerability Reporting, confirmar 2FA, publicar a primeira release valida, enviar o formulario, configurar a integracao apos aprovacao e aprovar manualmente cada assinatura.
- Limitacao: enquanto a aprovacao nao existir, os binarios seguem sem assinatura digital.

## Comandos validados

O runtime `Microsoft.NETCore.App 9.0.18` x64 foi instalado para executar os testes `net9.0` sem roll-forward.

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

Resultado: sucesso, 33 testes aprovados (30 unitarios e 3 de integracao).

```powershell
actionlint
```

Resultado: actionlint 1.7.12 validou `.github/workflows/ci.yml` e `.github/workflows/release.yml` sem diagnosticos.

```powershell
gitleaks detect --source . --redact --verbose
```

Resultado: sucesso, 9 commits verificados e nenhum leak encontrado.

```powershell
gitleaks dir . --redact --verbose
```

Resultado: sucesso, aproximadamente 6,91 MB do working tree verificados e nenhum leak encontrado.

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

- Revisar o diff local das correcoes.
- Autorizar separadamente commit e push para `main`.
- Confirmar o novo CI verde no GitHub antes de qualquer tag.
- Ativar Private Vulnerability Reporting.
- Ativar 2FA.
- Autorizar explicitamente a criacao da tag imutavel `v0.1.1`.
- Conferir a release, os artefatos Velopack e `SHA256SUMS.txt`.
- Enviar a solicitacao a SignPath Foundation.
- Configurar a integracao SignPath apos aprovacao.
- Aprovar manualmente cada solicitacao de assinatura.
