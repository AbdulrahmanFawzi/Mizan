using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Notifications;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50);

        builder.Ignore(n => n.Data);
        builder.Property<Dictionary<string, string>>("_data")
            .HasColumnName("data")
            .HasColumnType("jsonb")
            .HasConversion(
                d => JsonSerializer.Serialize(d, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default) ?? new(),
                new ValueComparer<Dictionary<string, string>>(
                    (a, b) => a!.Count == b!.Count && !a.Except(b).Any(),
                    d => d.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    d => new Dictionary<string, string>(d)));

        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
    }
}
