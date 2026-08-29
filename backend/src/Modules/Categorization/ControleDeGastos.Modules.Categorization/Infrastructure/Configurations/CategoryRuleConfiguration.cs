using ControleDeGastos.Modules.Categorization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Categorization.Infrastructure.Configurations;

internal sealed class CategoryRuleConfiguration : IEntityTypeConfiguration<CategoryRule>
{
    public void Configure(EntityTypeBuilder<CategoryRule> builder)
    {
        builder.ToTable("category_rules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();

        builder.Property(r => r.Keyword).HasMaxLength(80).IsRequired();

        builder.Property(r => r.Category).HasMaxLength(60).IsRequired();

        builder.Property(r => r.Priority).IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Ignore(r => r.DomainEvents);

        builder.HasIndex(r => new { r.UserId, r.Keyword }).IsUnique();
    }
}
