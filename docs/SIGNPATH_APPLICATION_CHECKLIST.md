# SignPath Foundation application checklist

- [ ] Repositorio principal publico
- [ ] Licenca MIT detectada pelo GitHub
- [ ] Codigo-fonte completo publicado
- [ ] Scripts de build publicados
- [ ] GitHub Actions publicado
- [ ] README completo
- [ ] Pagina de download documentada no proprio GitHub Releases
- [ ] Primeira release publica disponivel
- [ ] Politica de privacidade publicada
- [ ] Code signing policy publicada
- [ ] Papeis de committer/reviewer/approver publicados
- [ ] 2FA habilitado no GitHub
- [ ] 2FA preparado para a conta SignPath
- [ ] Build automatizado e verificavel
- [ ] Metadados do executavel consistentes
- [ ] Changelog disponivel
- [ ] Checksums SHA-256 publicados
- [ ] THIRD_PARTY_NOTICES revisado
- [ ] Nenhum segredo no codigo ou historico
- [ ] Release aponta para o codigo-fonte exato
- [ ] Uma release ja foi publicada no formato que devera ser assinado
- [ ] Formulario da SignPath Foundation enviado manualmente

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
- A solicitacao para a SignPath Foundation e manual.
- Cada assinatura deve exigir aprovacao manual do mantenedor.
- `trivy.exe` e componente upstream da Aqua Security e nao deve ser tratado como binario proprio do Package-Analyzer.
