namespace Codify.Domain.Exceptions;

/// <summary>Thrown when an operation conflicts with existing data (e.g. duplicate title). Maps to HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);
