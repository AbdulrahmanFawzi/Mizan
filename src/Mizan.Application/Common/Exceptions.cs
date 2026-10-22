namespace Mizan.Application.Common;

public sealed class NotFoundException(string resource) : Exception($"{resource} was not found.");

public sealed class ConflictException(string message) : Exception(message);

public sealed class ValidationException(string message) : Exception(message);

public sealed class AuthenticationFailedException(string message = "Invalid credentials.") : Exception(message);
