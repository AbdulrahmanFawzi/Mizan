using Mizan.Domain.Categorization;
using Mizan.Domain.Importing;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Importing;

public class DuplicateMatcherTests
{
    private static MatchCandidate C(decimal amount, string date, string? merchant = null) =>
        new(Guid.NewGuid(), Sar(amount), D(date), MerchantKey.From(merchant));

    private static readonly MatchCandidate Bank = C(50, "2026-10-20", "XYZ FUEL 12");

    [Fact]
    public void SameAmount_WithinThreeDays_Matches()
    {
        var manual = C(50, "2026-10-23");

        Assert.Equal(manual.Id, DuplicateMatcher.FindMatch(Bank, [manual], new HashSet<Guid>()));
    }

    [Theory]
    [InlineData(50.01, "2026-10-20")]   // different amount
    [InlineData(50, "2026-10-24")]      // 4 days apart
    public void OtherwiseNoMatch(decimal amount, string date)
    {
        Assert.Null(DuplicateMatcher.FindMatch(Bank, [C(amount, date)], new HashSet<Guid>()));
    }

    [Fact]
    public void ClosestDateWins_ThenTheSameMerchant()
    {
        var far = C(50, "2026-10-22", "xyz fuel");
        var near = C(50, "2026-10-21");
        var nearSameMerchant = C(50, "2026-10-19", "xyz fuel");

        Assert.Equal(nearSameMerchant.Id, DuplicateMatcher.FindMatch(Bank, [far, near, nearSameMerchant], new HashSet<Guid>()));
    }

    [Fact]
    public void RejectedPair_IsNeverSuggestedAgain()
    {
        var manual = C(50, "2026-10-20");

        Assert.Null(DuplicateMatcher.FindMatch(Bank, [manual], new HashSet<Guid> { manual.Id }));
    }
}

public class TransferPairMatcherTests
{
    private static readonly Guid Current = Guid.NewGuid();
    private static readonly Guid Card = Guid.NewGuid();

    private static TransferPairMatcher.Movement M(Guid account, decimal amount, string date) =>
        new(Guid.NewGuid(), account, Sar(amount), D(date));

    [Fact]
    public void SameAmountIntoAnotherOwnAccount_WithinTwoDays_IsATransfer()
    {
        var debit = M(Current, 1500, "2026-10-10");

        var result = TransferPairMatcher.FindOutgoingTransfers([debit], [M(Card, 1500, "2026-10-12")]);

        Assert.Equal(Card, result[debit.Id]);
    }

    [Fact]
    public void SameAccount_TooLate_OrDifferentAmount_IsNot()
    {
        var debit = M(Current, 1500, "2026-10-10");

        Assert.Empty(TransferPairMatcher.FindOutgoingTransfers([debit],
            [M(Current, 1500, "2026-10-10"), M(Card, 1500, "2026-10-13"), M(Card, 1499, "2026-10-10")]));
    }

    [Fact]
    public void EachIncomingMovement_PairsOnlyOnce()
    {
        var first = M(Current, 200, "2026-10-10");
        var second = M(Current, 200, "2026-10-10");

        var result = TransferPairMatcher.FindOutgoingTransfers([first, second], [M(Card, 200, "2026-10-10")]);

        Assert.Single(result);
    }
}
