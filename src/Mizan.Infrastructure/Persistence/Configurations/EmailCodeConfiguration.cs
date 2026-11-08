using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Application.Auth;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class EmailCodeConfiguration : IEntityTypeConfiguration<EmailCode>
{
    public void Configure(EntityTypeBuilder<EmailCode> builder)
    {
        builder.ToTable("email_codes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Purpose).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CodeHash).HasMaxLength(64);
        builder.HasIndex(c => new { c.UserId, c.Purpose, c.CreatedAt });

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
