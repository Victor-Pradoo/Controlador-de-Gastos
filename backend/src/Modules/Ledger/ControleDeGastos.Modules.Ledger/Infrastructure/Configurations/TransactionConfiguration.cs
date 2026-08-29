using ControleDeGastos.Modules.Ledger.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Ledger.Infrastructure.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();

        builder.Property(t => t.Kind).HasConversion<int>().IsRequired();

        builder.Property(t => t.Source).HasConversion<int>().IsRequired();

        builder.Property(t => t.Description).HasMaxLength(Transaction.MaxDescriptionLength).IsRequired();

        builder.Property(t => t.Category).HasMaxLength(Transaction.MaxCategoryLength).IsRequired();

        builder.ComplexProperty(t => t.Amount, amount =>
            amount.Property(m => m.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired());

        builder.Property(t => t.OccurredOn).IsRequired();

        builder.Property(t => t.ExternalId).HasMaxLength(200);

        builder.Property(t => t.CreatedAt).IsRequired();

        builder.Ignore(t => t.DomainEvents);

        // Consulta dominante do app: "lancamentos do usuario no mes".
        builder.HasIndex(t => new { t.UserId, t.OccurredOn });

        // Idempotencia da sincronizacao bancaria.
        builder.HasIndex(t => new { t.UserId, t.ExternalId })
            .IsUnique()
            .HasFilter("[ExternalId] IS NOT NULL");

        builder.HasIndex(t => new { t.UserId, t.RecurrenceId });
    }
}
