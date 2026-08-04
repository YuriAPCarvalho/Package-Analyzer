namespace TrivyProjectManager.Application.Abstractions;

public interface ISecretMaskingService
{
    string Mask(string? value);
}
