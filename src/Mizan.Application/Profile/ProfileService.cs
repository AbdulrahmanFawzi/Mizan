using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Budgets;
using Mizan.Domain.Common;
using Mizan.Domain.Users;

namespace Mizan.Application.Profile;

public sealed record ProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Currency,
    MoneyDto MonthlyIncome,
    int PeriodStartDay,
    bool EmailVerified);

public sealed record UpdateProfileCommand(string Name, decimal MonthlyIncome, int PeriodStartDay);

public sealed class ProfileService(
    IUserRepository users,
    IBankingCustomerRepository bankingCustomers,
    IOutbox outbox,
    IUnitOfWork unitOfWork)
{
    public async Task<ProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        return ToDto(user);
    }

    // UC-04, UC-11, and the period start day (BR-P4).
    public async Task<ProfileDto> UpdateAsync(Guid userId, UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        user.Rename(PersonName.From(command.Name));
        user.ChangeMonthlyIncome(Money.Of(command.MonthlyIncome, user.Currency));
        user.ChangePeriodStartDay(PeriodStartDay.From(command.PeriodStartDay));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(user);
    }

    // UC-06, BR-U3: the database cascades the delete to all of the user's data.
    // BR-C10: the bank provider is told in the background (outbox), so our data is deleted even if it's down.
    public async Task DeleteAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId, cancellationToken);

        // Deleting the customer at the provider removes its links and consents too.
        if (await bankingCustomers.GetAsync(userId, cancellationToken) is { } customer)
        {
            outbox.Enqueue(OutboxTypes.DeleteCustomer, new DeleteCustomerMessage(customer.ProviderCustomerId));
        }

        users.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");

    private static ProfileDto ToDto(User user) => new(
        user.Id,
        user.Name.Value,
        user.Email.Value,
        user.Currency.Code,
        MoneyDto.From(user.MonthlyIncome),
        user.PeriodStartDay.Value,
        user.IsEmailVerified);
}
