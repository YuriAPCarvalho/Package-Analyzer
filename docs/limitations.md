# Limitações Atuais

- Cadastro de projeto é direto após seleção de pasta; correções manuais ficam na aba `Configurações`.
- O dashboard usa o último scan concluído do projeto.
- A aba `Histórico` lista scans, mas abrir um scan antigo como visão isolada ainda não foi implementado.
- Misconfigurations e secrets são exibidos em abas próprias, mas os componentes visuais ainda são simples.
- Secrets são mascarados antes da persistência do finding e o JSON salvo é redigido nos campos `Secrets.Match` e `Code.Lines.Content`.
- Retenção automática remove scans antigos e seus arquivos associados conforme a configuração.
- O pacote `Microsoft.EntityFrameworkCore.Design` foi removido do runtime para evitar um target transitivo problemático neste ambiente; a migration inicial permanece versionada manualmente.
