namespace Mizan.Application.Common;

public sealed class NotFoundException(string resource) : Exception($"{resource} was not found.")
{
    public string Resource => resource;
}

public sealed class ConflictException(string message) : Exception(message);

public sealed class ValidationException(string message) : Exception(message);

// BR-U5: linking a bank needs a verified email.
public sealed class EmailNotVerifiedException() : Exception("Verify your email first.");

public sealed class AuthenticationFailedException(string message = "Invalid credentials.") : Exception(message);
