using ControleDeGastos.Modules.Budgeting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Budgeting.Infrastructure.Configurations;

internal sealed class BudgetSettingsConfiguration : IEntityTypeConfiguration<BudgetSettings>
{
    public void Configure(EntityTypeBuilder<BudgetSettings> builder)
    {
        builder.ToTable("budget_settings");

        // O Id do agregado e o proprio UserId: um registro de configuracao por usuario.
        builder.HasKey(s => s.Id);

        builder.ComplexProperty(s => s.Salary, salary =>
            salary.Property(m => m.Amount).HasColumnName("salary").HasPrecision(18, 2).IsRequired());

        builder.Property(s => s.ReserveRate).HasPrecision(5, 2).IsRequired();

        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.Ignore(s => s.DomainEvents);
        builder.Ignore(s => s.ReserveAmount);
        builder.Ignore(s => s.Available);
    }
}
