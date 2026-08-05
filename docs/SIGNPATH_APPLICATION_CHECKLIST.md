# SignPath Foundation application checklist

- [x] Repositorio principal publico
- [x] Licenca MIT detectada pelo GitHub
- [x] Codigo-fonte completo publicado
- [x] Scripts de build publicados
- [x] GitHub Actions publicado
- [x] README completo
- [x] Pagina de download documentada no proprio GitHub Releases
- [ ] Primeira release publica disponivel
- [x] Politica de privacidade publicada
- [x] Code signing policy publicada
- [x] Papeis de committer/reviewer/approver publicados
- [ ] 2FA habilitado no GitHub
- [ ] 2FA preparado para a conta SignPath
- [ ] Build automatizado e verificavel
- [x] Metadados do executavel consistentes
- [x] Changelog disponivel
- [ ] Checksums SHA-256 publicados
- [x] THIRD_PARTY_NOTICES revisado
- [x] Nenhum segredo no codigo ou historico
- [ ] Release aponta para o codigo-fonte exato
- [ ] Uma release ja foi publicada no formato que devera ser assinado
- [ ] Formulario da SignPath Foundation enviado manualmente

## Estado verificado em 2026-08-05

- O repositorio `YuriAPCarvalho/Package-Analyzer` esta publico e o GitHub detecta a licenca MIT.
- O repositorio `YuriAPCarvalho/Package-Analyzer-Download` nao foi encontrado e nao participa mais do fluxo.
- A tag `v0.1.0` permanece no commit `14d075a`, mas nenhuma GitHub Release foi publicada para ela.
- A correcao do workflow e o novo CI estao preparados localmente; o build automatizado so deve ser marcado como verificado depois de commit, push e CI verde.
- A primeira release valida planejada e `v0.1.1`, sem criacao de tag ate autorizacao explicita.
- Private Vulnerability Reporting permanece desativado e deve ser habilitado manualmente.
- Os artefatos permanecem sem assinatura; a integracao SignPath Foundation continua como planned / application pending.

## Informacoes para preencher manualmente

- URL do repositorio: `https://github.com/YuriAPCarvalho/Package-Analyzer`
- URL da pagina de download: `https://github.com/YuriAPCarvalho/Package-Analyzer/releases`
- Descricao do projeto: aplicacao desktop local-first para analise de seguranca de projetos com Trivy local, historico, comparacao de scans e recomendacoes de correcao.
- Licenca: MIT
- Responsavel: YuriAPCarvalho / Yuri Alexandre Pires de Carvalho
- Code signing policy: `CODE_SIGNING_POLICY.md`
- Politica de privacidade: `PRIVACY.md`
- Workflow de build: `.github/workflows/release.yml`
- Release de exemplo: preencher manualmente apos a primeira release publica.

## Pontos de atencao

- A ativacao de 2FA e manual.
- A ativacao de Private Vulnerability Reporting e manual.
- A solicitacao para a SignPath Foundation e manual.
- Cada assinatura deve exigir aprovacao manual do mantenedor.
- `trivy.exe` e componente upstream da Aqua Security e nao deve ser tratado como binario proprio do Package-Analyzer.
