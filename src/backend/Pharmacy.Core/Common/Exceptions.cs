namespace Pharmacy.Core.Common;

/// <summary>Base type for expected business errors that are translated into HTTP responses.</summary>
public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class ConflictException(string message) : DomainException(message);

public sealed class BusinessRuleException(string message) : DomainException(message);

public sealed class AuthenticationFailedException()
    : DomainException("Usuario o contraseña incorrectos, o la cuenta está bloqueada temporalmente.");

public sealed class ValidationException : DomainException
{
    public ValidationException(string field, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }

    public ValidationException(IDictionary<string, string[]> errors) : base("Uno o más datos no son válidos.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
