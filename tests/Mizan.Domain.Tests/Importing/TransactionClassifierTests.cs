using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;

namespace Mizan.Domain.Tests.Importing;

public class TransactionClassifierTests
{
    private static readonly AccountContext Counted = new(true, Currency.SAR);
    private static readonly CounterpartyAccount MyCard = new(Guid.NewGuid(), AccountKind.CreditCard);
    private static readonly CounterpartyAccount MySavings = new(Guid.NewGuid(), AccountKind.Savings);

    private static Classification Classify(
        TransactionDirection direction = TransactionDirection.Debit,
        BankSubType subType = BankSubType.None,
        CounterpartyAccount? counterparty = null,
        string currency = "SAR",
        AccountContext? account = null) =>
        TransactionClassifier.Classify(direction, subType, Currency.From(currency), account ?? Counted, counterparty, Currency.SAR);

    [Fact]
    public void Purchase_IsSpending()
    {
        Assert.Equal(new Classification(Treatment.Spending, null), Classify());
    }

    [Fact]
    public void AccountThatDoesntCount_IsOutsideTheBudget()
    {
        Assert.Equal(Treatment.OutsideBudget, Classify(account: new AccountContext(false, Currency.SAR)).Treatment);
    }

    [Fact]
    public void OtherCurrency_IsOutsideTheBudget()
    {
        Assert.Equal(Treatment.OutsideBudget, Classify(currency: "USD").Treatment);
    }

    [Theory]
    [InlineData(BankSubType.Refund)]
    [InlineData(BankSubType.Reversal)]
    public void IncomingRefund_IsRefund(BankSubType subType)
    {
        Assert.Equal(new Classification(Treatment.Refund, null), Classify(TransactionDirection.Credit, subType));
    }

    [Fact]
    public void OtherIncomingMoney_IsIncoming()
    {
        Assert.Equal(new Classification(Treatment.Incoming, null), Classify(TransactionDirection.Credit));
    }

    [Fact]
    public void PaymentToMyOwnCard_AsksTheUser()
    {
        Assert.Equal(new Classification(Treatment.Spending, ReviewReason.PossibleTransfer), Classify(counterparty: MyCard));
    }

    [Fact]
    public void MoneyArrivingFromMyOwnCard_IsOwnTransfer_WithNoQuestion()
    {
        Assert.Equal(new Classification(Treatment.OwnTransfer, null), Classify(TransactionDirection.Credit, counterparty: MyCard));
    }

    [Fact]
    public void MovingMoneyIntoMySavingsAccount_IsSaving_WithNoQuestion()
    {
        Assert.Equal(new Classification(Treatment.Spending, null, IsSaving: true), Classify(counterparty: MySavings));
    }

    [Fact]
    public void TakingMoneyOutOfMySavings_LowersSaving()
    {
        Assert.Equal(new Classification(Treatment.Refund, null, IsSaving: true), Classify(TransactionDirection.Credit, counterparty: MySavings));
    }

    [Fact]
    public void TransferToSomeoneElse_IsSpending()
    {
        Assert.Equal(new Classification(Treatment.Spending, null), Classify());
    }
}
