using ControleDeGastos.Modules.Banking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Banking.Infrastructure.Configurations;

internal sealed class BankConnectionConfiguration : IEntityTypeConfiguration<BankConnection>
{
    public void Configure(EntityTypeBuilder<BankConnection> builder)
    {
        builder.ToTable("bank_connections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();

        builder.Property(c => c.Provider).HasMaxLength(40).IsRequired();

        builder.Property(c => c.ExternalItemId).HasMaxLength(120).IsRequired();

        builder.Property(c => c.InstitutionName).HasMaxLength(120).IsRequired();

        builder.Property(c => c.Status).HasConversion<int>().IsRequired();

        builder.Property(c => c.LastError).HasMaxLength(400);

        builder.Ignore(c => c.DomainEvents);

        builder.HasIndex(c => new { c.UserId, c.ExternalItemId }).IsUnique();
    }
}
