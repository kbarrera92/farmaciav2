namespace Pharmacy.Core.Common;

/// <summary>User executing the current operation; null when anonymous or running outside a request.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
}
