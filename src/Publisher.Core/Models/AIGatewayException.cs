namespace Publisher.Core.Models;

/// <summary>Raised when a call to the AI Gateway fails (network, auth, or provider error).</summary>
public sealed class AIGatewayException : Exception
{
    public string? Code { get; }
    public string? Provider { get; }

    public AIGatewayException(string message, string? code = null, string? provider = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Provider = provider;
    }
}
