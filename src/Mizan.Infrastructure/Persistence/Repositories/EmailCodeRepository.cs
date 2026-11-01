using Microsoft.EntityFrameworkCore;
using Mizan.Application.Abstractions;
using Mizan.Application.Auth;

namespace Mizan.Infrastructure.Persistence.Repositories;

internal sealed class EmailCodeRepository(MizanDbContext db) : IEmailCodeRepository
{
    public Task<EmailCode?> GetLatestAsync(Guid userId, EmailCodePurpose purpose, CancellationToken cancellationToken) =>
        db.EmailCodes
            .Where(c => c.UserId == userId && c.Purpose == purpose)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(EmailCode code) => db.EmailCodes.Add(code);
}
