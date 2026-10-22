using Mizan.Domain.Budgets;
using Mizan.Domain.Common;
using Mizan.Domain.Users;
using static Mizan.Domain.Tests.TestData;

namespace Mizan.Domain.Tests.Users;

public class UserTests
{
    private static User NewUser(decimal income = 10_000) => User.Register(
        PersonName.From("Ali"),
        Email.From("ali@example.com"),
        "hash",
        Sar(income),
        PeriodStartDay.From(27),
        Now);

    [Fact]
    public void Register_RaisesUserRegistered()
    {
        var user = NewUser();

        Assert.Equal(Currency.SAR, user.Currency);
        Assert.Equal(new UserRegistered(user.Id), Assert.Single(user.DomainEvents));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Register_NonPositiveIncome_Throws(decimal income)
    {
        Assert.Throws<DomainException>(() => NewUser(income));
    }

    [Fact]
    public void Register_UnsupportedCurrency_Throws()
    {
        Assert.Throws<DomainException>(() => User.Register(
            PersonName.From("Ali"), Email.From("ali@example.com"), "hash",
            Money.Of(10_000, Currency.From("USD")), PeriodStartDay.From(1), Now));
    }

    [Fact]
    public void ChangeMonthlyIncome_RaisesEventOnlyWhenChanged()
    {
        var user = NewUser();
        user.ClearDomainEvents();

        user.ChangeMonthlyIncome(Sar(10_000));
        Assert.Empty(user.DomainEvents);

        user.ChangeMonthlyIncome(Sar(12_000));
        Assert.Equal(Sar(12_000), user.MonthlyIncome);
        Assert.Equal(new MonthlyIncomeChanged(user.Id, Sar(12_000)), Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void ChangeMonthlyIncome_ToZero_Throws()
    {
        Assert.Throws<DomainException>(() => NewUser().ChangeMonthlyIncome(Sar(0)));
    }
}
