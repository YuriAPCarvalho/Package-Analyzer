# Contributing

Obrigado por considerar contribuir com o Package-Analyzer.

## Issues

Use issues para bugs, melhorias e discussoes tecnicas. Antes de abrir uma issue, verifique se ja existe uma conversa relacionada.

Inclua somente logs, screenshots e exemplos sanitizados. Nunca inclua tokens, senhas, chaves privadas, dados de clientes, caminhos sensiveis ou relatorios com secrets reais.

## Branches

Use nomes curtos e descritivos:

- `feat/<descricao>`
- `fix/<descricao>`
- `docs/<descricao>`
- `chore/<descricao>`

## Commits

Prefixos sugeridos:

- `feat:`
- `fix:`
- `docs:`
- `refactor:`
- `test:`
- `build:`
- `ci:`
- `chore:`

## Desenvolvimento local

```powershell
dotnet restore TrivyProjectManager.sln -m:1 -nr:false
dotnet build TrivyProjectManager.sln --configuration Release --no-restore -m:1 -nr:false
dotnet test TrivyProjectManager.sln --configuration Release --no-build -m:1 -nr:false
```

## Pull requests

Pull requests devem:

- explicar o problema e a solucao;
- manter o projeto compilavel;
- atualizar testes quando houver mudanca de comportamento;
- atualizar documentacao quando a experiencia do usuario, privacidade, release ou instalacao mudar;
- incluir screenshot para mudancas visuais;
- confirmar que nenhuma credencial foi adicionada.

Mudancas em `.github/workflows/`, scripts de release, instalador, updater, assinatura de codigo ou politicas de seguranca exigem revisao especial do mantenedor.
