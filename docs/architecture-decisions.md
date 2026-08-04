# Decisões Arquiteturais

## Local-first

A aplicação não implementa login, telemetria, analytics, uploads ou enriquecimento externo automático. O único componente que pode acessar internet é o Trivy local, para atualizar a base pública de vulnerabilidades.

## Separação em Camadas

- `Domain` contém entidades e enums sem dependências externas.
- `Application` contém contratos, DTOs do Trivy, parser tolerante, deduplicação, contadores, comparação e validações.
- `Infrastructure` implementa EF Core SQLite, execução de processos, detecção de projetos, paths, retenção e Trivy.
- `App` contém Avalonia, ViewModels, DI, dialogs e logs locais.

## Execução de Processos

Comandos são persistidos com executável e argumentos separados. A execução usa `ProcessStartInfo.ArgumentList`, `UseShellExecute=false`, stdout/stderr assíncronos, timeout e cancelamento.

## Deduplicação

A chave lógica de finding usa:

```text
FindingType | VulnerabilityId ou título/target | PackageName | InstalledVersion
```

Ocorrências em targets diferentes são armazenadas em `FindingOccurrence`, evitando inflar o resumo principal.

## SQLite

O banco fica em `%LOCALAPPDATA%\TrivyProjectManager\data\`. Relatórios e logs podem ficar no armazenamento central configurado ou em `.security/trivy/` dentro do projeto.

## Migrations

A migration inicial é mantida no projeto de infraestrutura. Como foi criada manualmente neste MVP, a aplicação ignora apenas o warning de pending model changes do snapshot manual.
