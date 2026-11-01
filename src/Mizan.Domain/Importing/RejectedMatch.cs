namespace Mizan.Domain.Importing;

// BR-D6: the user said these two are different purchases, so we never suggest them together again.
public sealed record RejectedMatch(Guid BankTransactionId, Guid ManualTransactionId);
