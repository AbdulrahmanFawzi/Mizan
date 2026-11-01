using Mizan.Application.Abstractions;
using Mizan.Application.Common;
using Mizan.Domain.Budgets;
using Mizan.Domain.Common;
using Mizan.Domain.Users;

namespace Mizan.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    EmailCodes emailCodes,
    IUnitOfWork unitOfWork,
    Clock clock)
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<AuthTokens> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        EnsureValidPassword(command.Password);
        var email = Email.From(command.Email);

        // BR-U1 (the database unique index is the final guard against two simultaneous sign-ups).
        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = User.Register(
            PersonName.From(command.Name),
            email,
            passwordHasher.Hash(command.Password),
            Money.Of(command.MonthlyIncome, Currency.SAR),
            PeriodStartDay.From(command.PeriodStartDay),
            clock.Now);

        users.Add(user);
        await emailCodes.SendAsync(user, EmailCodePurpose.VerifyEmail, cancellationToken);
        var tokens = IssueTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    public async Task<AuthTokens> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(command.Email);
        }
        catch (DomainException)
        {
            throw new AuthenticationFailedException();
        }

        var user = await users.GetByEmailAsync(email, cancellationToken);

        // Same error whether the email or the password is wrong, so emails can't be discovered.
        var check = user is null ? PasswordCheck.Failed : passwordHasher.Verify(user.PasswordHash, command.Password);
        if (user is null || check == PasswordCheck.Failed)
        {
            throw new AuthenticationFailedException();
        }

        if (check == PasswordCheck.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(command.Password));
        }

        var tokens = IssueTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    // Rotation: every refresh token works once. Reusing an old one means it was probably stolen,
    // so all of the user's sessions are ended.
    public async Task<AuthTokens> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        var now = clock.Now;
        var stored = await refreshTokens.GetByHashAsync(RefreshToken.HashOf(command.RefreshToken), cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid refresh token.");

        if (stored.IsRevoked)
        {
            foreach (var token in await refreshTokens.ListActiveAsync(stored.UserId, now, cancellationToken))
            {
                token.Revoke(now);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException("Invalid refresh token.");
        }

        if (stored.IsExpired(now))
        {
            throw new AuthenticationFailedException("Refresh token has expired.");
        }

        var user = await users.GetAsync(stored.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid refresh token.");

        stored.Revoke(now);
        var tokens = IssueTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    public async Task LogoutAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        var stored = await refreshTokens.GetByHashAsync(RefreshToken.HashOf(command.RefreshToken), cancellationToken);
        if (stored is null)
        {
            return;
        }

        stored.Revoke(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // UC-05: always "done", so the answer doesn't reveal whether an account exists.
    public async Task ForgotPasswordAsync(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        if (await FindByEmailAsync(command.Email, cancellationToken) is not { } user)
        {
            return;
        }

        await emailCodes.SendAsync(user, EmailCodePurpose.ResetPassword, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // UC-05: the code proves the email is theirs, so it also verifies it. Every other session is ended,
    // in case someone else had the old password.
    public async Task<AuthTokens> ResetPasswordAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        EnsureValidPassword(command.NewPassword);
        var user = await FindByEmailAsync(command.Email, cancellationToken);

        if (user is null || !await emailCodes.TryUseAsync(user, EmailCodePurpose.ResetPassword, command.Code, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);   // keeps the failed attempt
            throw new ValidationException(WrongCode);
        }

        user.ChangePasswordHash(passwordHasher.Hash(command.NewPassword));
        user.VerifyEmail(clock.Now);
        foreach (var token in await refreshTokens.ListActiveAsync(user.Id, clock.Now, cancellationToken))
        {
            token.Revoke(clock.Now);
        }

        var tokens = IssueTokens(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    // UC-07: verify the email (needed before linking a bank, BR-U5)
    public async Task SendVerificationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        if (user.IsEmailVerified)
        {
            return;
        }

        await emailCodes.SendAsync(user, EmailCodePurpose.VerifyEmail, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task VerifyEmailAsync(Guid userId, VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User");
        if (user.IsEmailVerified)
        {
            return;
        }

        if (!await emailCodes.TryUseAsync(user, EmailCodePurpose.VerifyEmail, command.Code, cancellationToken))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ValidationException(WrongCode);
        }

        user.VerifyEmail(clock.Now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private const string WrongCode = "The code is wrong or has expired.";

    private static void EnsureValidPassword(string password)
    {
        if (password.Length is < MinPasswordLength or > MaxPasswordLength)
        {
            throw new ValidationException(
                $"Password must be between {MinPasswordLength} and {MaxPasswordLength} characters.");
        }
    }

    private async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            return await users.GetByEmailAsync(Email.From(email), cancellationToken);
        }
        catch (DomainException)
        {
            return null;
        }
    }

    private AuthTokens IssueTokens(User user)
    {
        var accessToken = accessTokenIssuer.Issue(user);
        var (refreshToken, plainText) = RefreshToken.Issue(user.Id, clock.Now, RefreshTokenLifetime);
        refreshTokens.Add(refreshToken);

        return new AuthTokens(accessToken.Token, accessToken.ExpiresAt, plainText, refreshToken.ExpiresAt);
    }
}
