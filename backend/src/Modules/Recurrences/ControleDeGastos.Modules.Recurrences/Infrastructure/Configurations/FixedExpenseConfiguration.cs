using ControleDeGastos.Modules.Recurrences.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Recurrences.Infrastructure.Configurations;

internal sealed class FixedExpenseConfiguration : IEntityTypeConfiguration<FixedExpense>
{
    public void Configure(EntityTypeBuilder<FixedExpense> builder)
    {
        builder.ToTable("fixed_expenses");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.UserId).IsRequired();

        builder.Property(f => f.Description).HasMaxLength(FixedExpense.MaxDescriptionLength).IsRequired();

        builder.Property(f => f.Category).HasMaxLength(60).IsRequired();

        builder.ComplexProperty(f => f.Amount, amount =>
            amount.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired());

        builder.Property(f => f.DayOfMonth).IsRequired();

        builder.Property(f => f.IsActive).IsRequired();

        builder.Property(f => f.CreatedAt).IsRequired();

        builder.Ignore(f => f.DomainEvents);

        builder.HasIndex(f => new { f.UserId, f.IsActive });
    }
}
