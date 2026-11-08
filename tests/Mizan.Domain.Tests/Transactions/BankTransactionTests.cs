using Mizan.Domain.Banking;
using Mizan.Domain.Common;
using Mizan.Domain.Importing;
using Mizan.Domain.Transactions;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Transactions;

public class BankTransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid Food = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();
    private static readonly BankReference Ref = new(Guid.NewGuid(), Guid.NewGuid(), "tx-1");

    internal static BankTransactionFacts Facts(
        decimal amount = 120,
        TransactionDirection direction = TransactionDirection.Debit,
        BankStatus status = BankStatus.Booked,
        string date = "2026-10-29",
        decimal? fee = null,
        string? merchant = "PANDA 0012") =>
        new("tx-1", Sar(amount), direction, status, D(date), merchant, null, BankSubType.None, null,
            fee is null ? null : Sar(fee.Value), IsCashWithdrawal: false);

    private static Transaction Import(
        BankTransactionFacts? facts = null,
        Treatment treatment = Treatment.Spending,
        ReviewReason? review = null) =>
        Transaction.ImportFromBank(UserId, Ref, facts ?? Facts(), treatment, review, Food, CategorySource.Provider, Now);

    private static Transaction Manual(decimal amount = 120) =>
        Transaction.Record(UserId, Sar(amount), Other, D("2026-10-28"), "Panda", null, Today, Now);

    [Fact]
    public void Import_RaisesNoEventPerTransaction()
    {
        Assert.Empty(Import().DomainEvents);
    }

    [Fact]
    public void Import_FutureBankDate_IsAccepted()
    {
        Assert.Equal(D("2026-11-02"), Import(Facts(date: "2026-11-02")).Date);
    }

    [Theory]
    [InlineData(Treatment.Spending, 120)]
    [InlineData(Treatment.Refund, -120)]
    [InlineData(Treatment.OwnTransfer, 0)]
    [InlineData(Treatment.Incoming, 0)]
    [InlineData(Treatment.OutsideBudget, 0)]
    public void BudgetEffect_FollowsTheTreatment(Treatment treatment, decimal expected)
    {
        Assert.Equal(Sar(expected), Import(treatment: treatment).BudgetEffect);
    }

    [Fact]
    public void BudgetEffect_AddsTheSeparateFee()
    {
        Assert.Equal(Sar(121.15m), Import(Facts(fee: 1.15m)).BudgetEffect);
    }

    [Fact]
    public void BudgetEffect_PossibleTransfer_CountsNothingUntilAnswered()
    {
        var transaction = Import(review: ReviewReason.PossibleTransfer);
        Assert.Equal(Sar(0), transaction.BudgetEffect);

        transaction.ResolveTransfer(isOwnTransfer: false, Now);

        Assert.Equal(Sar(120), transaction.BudgetEffect);
        Assert.Null(transaction.Review);
    }

    [Fact]
    public void ResolveTransfer_Yes_IsNeverSpending()
    {
        var transaction = Import(review: ReviewReason.PossibleTransfer);

        transaction.ResolveTransfer(isOwnTransfer: true, Now);

        Assert.Equal(Treatment.OwnTransfer, transaction.Treatment);
        Assert.Equal(Sar(0), transaction.BudgetEffect);
    }

    [Fact]
    public void Excluded_CountsNothing_AndCanBeIncludedAgain()
    {
        var transaction = Import();

        transaction.Exclude(Now);
        Assert.Equal(Sar(0), transaction.BudgetEffect);

        transaction.Include(Now);
        Assert.Equal(Sar(120), transaction.BudgetEffect);
    }

    [Fact]
    public void BankTransaction_CantBeEditedOrDeleted()
    {
        var transaction = Import();

        Assert.Throws<DomainException>(() => transaction.Update(Sar(1), Food, Today, null, Today, Now));
        Assert.Throws<DomainException>(transaction.Delete);
    }

    [Fact]
    public void ManualExpense_CantBeExcluded()
    {
        Assert.Throws<DomainException>(() => Manual().Exclude(Now));
    }

    [Fact]
    public void BankUpdate_KeepsTheUsersChoices()
    {
        var transaction = Import(Facts(status: BankStatus.Pending));
        transaction.Recategorize(Other, CategorySource.User, Now);
        transaction.SetNote("Dinner with family", Now);
        transaction.Exclude(Now);

        transaction.ApplyBankUpdate(Facts(amount: 118, status: BankStatus.Booked), Now);

        Assert.Equal(Sar(118), transaction.Amount);
        Assert.Equal(BankStatus.Booked, transaction.BankStatus);
        Assert.Equal(Other, transaction.CategoryId);
        Assert.Equal(CategorySource.User, transaction.CategorySource);
        Assert.Equal("Dinner with family", transaction.Note);
        Assert.True(transaction.IsExcluded);
    }

    [Fact]
    public void BankUpdate_RaisesUpdatedOnlyWhenTheBudgetIsAffected()
    {
        var transaction = Import();

        Assert.False(transaction.ApplyBankUpdate(Facts(merchant: "Panda Riyadh"), Now));
        Assert.Empty(transaction.DomainEvents);

        Assert.True(transaction.ApplyBankUpdate(Facts(amount: 99), Now));
        Assert.IsType<TransactionUpdated>(Assert.Single(transaction.DomainEvents));
    }

    [Fact]
    public void Discard_RaisesDeleted()
    {
        var transaction = Import();

        transaction.Discard();

        Assert.IsType<TransactionDeleted>(Assert.Single(transaction.DomainEvents));
    }

    [Fact]
    public void PossibleDuplicate_OnlyTheBankSideCounts()
    {
        var bank = Import();
        var manual = Manual();

        bank.FlagPossibleDuplicate(manual, Now);

        Assert.Equal(Sar(120), bank.BudgetEffect);
        Assert.Equal(Sar(0), manual.BudgetEffect);
        Assert.Equal(manual.Id, bank.ReviewCandidateId);
        Assert.Equal(bank.Id, manual.PossibleDuplicateOf);
    }

    [Fact]
    public void ConfirmDuplicate_TakesTheManualCategory_AndRemovesTheManual()
    {
        var bank = Import();
        var manual = Manual();
        bank.FlagPossibleDuplicate(manual, Now);
        manual.ClearDomainEvents();

        bank.ConfirmDuplicate(manual, Now);

        Assert.Equal(Other, bank.CategoryId);
        Assert.Equal(CategorySource.User, bank.CategorySource);
        Assert.Null(bank.Review);
        Assert.IsType<TransactionDeleted>(Assert.Single(manual.DomainEvents));
    }

    [Fact]
    public void RejectDuplicate_BothCount()
    {
        var bank = Import();
        var manual = Manual();
        bank.FlagPossibleDuplicate(manual, Now);

        bank.RejectDuplicate(manual, Now);

        Assert.Equal(Sar(120), bank.BudgetEffect);
        Assert.Equal(Sar(120), manual.BudgetEffect);
    }

    [Fact]
    public void Duplicate_DecisionNeedsTheRightPair()
    {
        var bank = Import();
        bank.FlagPossibleDuplicate(Manual(), Now);

        Assert.Throws<DomainException>(() => bank.ConfirmDuplicate(Manual(), Now));
    }

    [Fact]
    public void Reclassify_IntoSavings_MovesTheCategory_UnlessTheUserChoseOne()
    {
        var savings = Guid.NewGuid();
        var automatic = Import(treatment: Treatment.OutsideBudget);
        var chosen = Import(treatment: Treatment.OutsideBudget);
        chosen.Recategorize(Other, CategorySource.User, Now);

        automatic.Reclassify(new Classification(Treatment.Spending, null, IsSaving: true), savings, Now);
        chosen.Reclassify(new Classification(Treatment.Spending, null, IsSaving: true), savings, Now);

        Assert.Equal(savings, automatic.CategoryId);
        Assert.Equal(Sar(120), automatic.BudgetEffect);
        Assert.Equal(Other, chosen.CategoryId);
    }

    [Fact]
    public void Reclassify_OutOfTheBudget_DropsTheTransferQuestion()
    {
        var transaction = Import(review: ReviewReason.PossibleTransfer);

        transaction.Reclassify(new Classification(Treatment.OutsideBudget, null), Guid.NewGuid(), Now);

        Assert.Null(transaction.Review);
        Assert.Equal(Sar(0), transaction.BudgetEffect);
        Assert.IsType<TransactionUpdated>(Assert.Single(transaction.DomainEvents));
    }

    [Fact]
    public void Reclassify_WithNoChange_RaisesNothing()
    {
        var transaction = Import();

        transaction.Reclassify(new Classification(Treatment.Spending, null), Guid.NewGuid(), Now);

        Assert.Empty(transaction.DomainEvents);
    }

    [Fact]
    public void ManualExpense_DefaultsToUserSpending()
    {
        var manual = Manual();

        Assert.Equal(TransactionSource.Manual, manual.Source);
        Assert.Equal(Treatment.Spending, manual.Treatment);
        Assert.Equal(Sar(120), manual.BudgetEffect);
        Assert.Equal("panda", manual.MerchantKey!.Value);
    }
}
