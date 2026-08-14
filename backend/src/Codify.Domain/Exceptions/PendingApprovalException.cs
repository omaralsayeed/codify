namespace Codify.Domain.Exceptions;

/// <summary>
/// Thrown when a user with <c>Pending</c> status attempts to log in
/// before an admin has approved their account.
/// Maps to HTTP 403 with error code ACCOUNT_PENDING.
/// </summary>
public class PendingApprovalException(string message) : Exception(message);
