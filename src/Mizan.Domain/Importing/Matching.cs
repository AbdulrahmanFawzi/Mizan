using Mizan.Domain.Categorization;
using Mizan.Domain.Common;

namespace Mizan.Domain.Importing;

// The few facts matching needs about a transaction.
public sealed record MatchCandidate(Guid Id, Money Amount, DateOnly Date, MerchantKey? Merchant);

// BR-D1, BR-D2, BR-D6, BR-D7: does a manual expense and a bank transaction look like the same purchase?
public static class DuplicateMatcher
{
    public const int MaxDaysApart = 3;

    // Finds the best candidate for `target` (either side can be the target):
    // same amount exactly, at most 3 days apart, not already rejected.
    // Closest date wins; on a tie, a matching merchant wins.
    public static Guid? FindMatch(MatchCandidate target, IEnumerable<MatchCandidate> candidates, IReadOnlySet<Guid> rejected)
    {
        ArgumentNullException.ThrowIfNull(target);

        return candidates
            .Where(c => c.Amount == target.Amount
                && Math.Abs(c.Date.DayNumber - target.Date.DayNumber) <= MaxDaysApart
                && !rejected.Contains(c.Id))
            .OrderBy(c => Math.Abs(c.Date.DayNumber - target.Date.DayNumber))
            .ThenByDescending(c => c.Merchant is not null && c.Merchant == target.Merchant)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefault();
    }
}

// BR-R1 (fallback when the bank gives no IBAN): money left one own account and the same
// amount arrived in another own account within two days.
public static class TransferPairMatcher
{
    public const int MaxDaysApart = 2;

    public sealed record Movement(Guid Id, Guid AccountId, Money Amount, DateOnly Date);

    // For each outgoing movement that has a matching incoming one: the account the money went into.
    // Each incoming movement is used once.
    public static IReadOnlyDictionary<Guid, Guid> FindOutgoingTransfers(IEnumerable<Movement> outgoing, IEnumerable<Movement> incoming)
    {
        var available = incoming.ToList();
        var result = new Dictionary<Guid, Guid>();

        foreach (var debit in outgoing.OrderBy(o => o.Date))
        {
            var match = available
                .Where(c => c.AccountId != debit.AccountId
                    && c.Amount == debit.Amount
                    && Math.Abs(c.Date.DayNumber - debit.Date.DayNumber) <= MaxDaysApart)
                .OrderBy(c => Math.Abs(c.Date.DayNumber - debit.Date.DayNumber))
                .FirstOrDefault();

            if (match is not null)
            {
                available.Remove(match);
                result.Add(debit.Id, match.AccountId);
            }
        }

        return result;
    }
}
