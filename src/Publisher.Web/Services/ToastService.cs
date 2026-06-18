namespace Publisher.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed record ToastMessage(Guid Id, string Message, ToastLevel Level);

/// <summary>
/// Scoped (per-circuit) toast service. Components inject this, subscribe to
/// <see cref="OnShow"/> and render Bootstrap toasts. Call <see cref="Show"/> from
/// any interactive component to surface a transient message.
/// </summary>
public sealed class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Show(string message, ToastLevel level = ToastLevel.Info)
        => OnShow?.Invoke(new ToastMessage(Guid.NewGuid(), message, level));

    public void ShowSuccess(string message) => Show(message, ToastLevel.Success);
    public void ShowError(string message) => Show(message, ToastLevel.Error);
    public void ShowInfo(string message) => Show(message, ToastLevel.Info);
    public void ShowWarning(string message) => Show(message, ToastLevel.Warning);
}
