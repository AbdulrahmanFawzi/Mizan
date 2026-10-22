using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mizan.Domain.Categories;
using Mizan.Domain.Users;

namespace Mizan.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasConversion(n => n.Value, v => CategoryName.From(v))
            .HasMaxLength(CategoryName.MaxLength);

        builder.Property(c => c.NormalizedName).HasMaxLength(CategoryName.MaxLength);
        builder.HasIndex(c => new { c.UserId, c.NormalizedName }).IsUnique(); // BR-B1

        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
