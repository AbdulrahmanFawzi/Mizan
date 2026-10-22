using Mizan.Domain.Common;
using Mizan.Domain.Transactions;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Transactions;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid Fuel = Guid.NewGuid();

    private static Transaction Record(decimal amount = 50, string? date = null, string? merchant = "XYZ Fuel") =>
        Transaction.Record(UserId, Sar(amount), Fuel, date is null ? Today : D(date), merchant, null, Today, Now);

    [Fact]
    public void Record_RaisesTransactionRecorded()
    {
        var transaction = Record();

        Assert.Equal(
            new TransactionRecorded(transaction.Id, UserId, Fuel, Today, null),
            Assert.Single(transaction.DomainEvents));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Record_NonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<DomainException>(() => Record(amount));
    }

    [Fact]
    public void Record_FutureDate_Throws()
    {
        Assert.Throws<DomainException>(() => Record(date: "2026-10-31"));
    }

    [Fact]
    public void Record_PastDate_IsAllowed()
    {
        Assert.Equal(D("2026-01-15"), Record(date: "2026-01-15").Date);
    }

    [Fact]
    public void Record_EmptyCategory_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Transaction.Record(UserId, Sar(50), Guid.Empty, Today, null, null, Today, Now));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData(" Al Baik ", "Al Baik")]
    public void Record_MerchantIsOptionalAndTrimmed(string? merchant, string? expected)
    {
        Assert.Equal(expected, Record(merchant: merchant).Merchant);
    }

    [Fact]
    public void Update_RaisesTransactionUpdated()
    {
        var transaction = Record();
        transaction.ClearDomainEvents();
        var food = Guid.NewGuid();

        transaction.Update(Sar(70), food, D("2026-10-29"), null, Today, Now);

        Assert.Equal(Sar(70), transaction.Amount);
        Assert.Equal(
            new TransactionUpdated(transaction.Id, UserId, food, D("2026-10-29"), null),
            Assert.Single(transaction.DomainEvents));
    }
}
