namespace Mizan.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string Body);

// Sends one email. Called from the outbox worker, never inside a request (BR-U4).
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
