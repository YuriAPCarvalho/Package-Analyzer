using TrivyProjectManager.Application.DTOs;
using TrivyProjectManager.Domain.Enums;
using UpdateStatus = TrivyProjectManager.Application.DTOs.ApplicationUpdateStatus;

namespace TrivyProjectManager.Application.Services;

public static class DisplayTextService
{
    public static string Severity(FindingSeverity severity) => severity switch
    {
        FindingSeverity.Critical => "Crítica",
        FindingSeverity.High => "Alta",
        FindingSeverity.Medium => "Média",
        FindingSeverity.Low => "Baixa",
        FindingSeverity.Unknown => "Desconhecida",
        _ => severity.ToString()
    };

    public static string LifecycleStatus(FindingLifecycleStatus status) => status switch
    {
        FindingLifecycleStatus.New => "Nova",
        FindingLifecycleStatus.Existing => "Existente",
        FindingLifecycleStatus.Resolved => "Resolvida",
        FindingLifecycleStatus.Regression => "Regressão",
        FindingLifecycleStatus.Ignored => "Ignorada",
        _ => status.ToString()
    };

    public static string ScanStatus(ScanStatus status) => status switch
    {
        Domain.Enums.ScanStatus.Pending => "Pendente",
        Domain.Enums.ScanStatus.Running => "Executando",
        Domain.Enums.ScanStatus.Succeeded => "Concluído",
        Domain.Enums.ScanStatus.Failed => "Falhou",
        Domain.Enums.ScanStatus.Cancelled => "Cancelado",
        Domain.Enums.ScanStatus.TimedOut => "Tempo esgotado",
        _ => status.ToString()
    };

    public static string FindingType(FindingType type) => type switch
    {
        Domain.Enums.FindingType.Vulnerability => "Vulnerabilidade",
        Domain.Enums.FindingType.Misconfiguration => "Configuração incorreta",
        Domain.Enums.FindingType.Secret => "Segredo",
        Domain.Enums.FindingType.License => "Licença",
        _ => type.ToString()
    };

    public static string ApplicationUpdateStatus(ApplicationUpdateStatus status) => status switch
    {
        UpdateStatus.Idle => "Aguardando",
        UpdateStatus.Checking => "Verificando",
        UpdateStatus.UpToDate => "Atualizado",
        UpdateStatus.UpdateAvailable => "Atualização disponível",
        UpdateStatus.Downloading => "Baixando",
        UpdateStatus.Applying => "Aplicando",
        UpdateStatus.Failed => "Falhou",
        UpdateStatus.NotInstalled => "Não instalado via Velopack",
        _ => status.ToString()
    };
}
