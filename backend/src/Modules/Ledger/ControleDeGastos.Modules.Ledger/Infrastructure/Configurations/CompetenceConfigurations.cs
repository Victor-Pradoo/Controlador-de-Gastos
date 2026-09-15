using ControleDeGastos.Modules.Ledger.Domain.Competence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleDeGastos.Modules.Ledger.Infrastructure.Configurations;

internal sealed class CompetenceSettingsConfiguration : IEntityTypeConfiguration<CompetenceSettings>
{
    public void Configure(EntityTypeBuilder<CompetenceSettings> builder)
    {
        builder.ToTable("competence_settings");

        // O Id do agregado e o proprio UserId: uma regra de virada por usuario.
        builder.HasKey(s => s.Id);

        // Nulo e um estado de negocio, nao ausencia de dado: significa
        // "competencia = mes do calendario", que e o padrao de quem nunca configurou.
        builder.Property(s => s.ClosingDay);

        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}

internal sealed class CompetenceClosureConfiguration : IEntityTypeConfiguration<CompetenceClosure>
{
    public void Configure(EntityTypeBuilder<CompetenceClosure> builder)
    {
        builder.ToTable("competence_closures");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();

        builder.Property(c => c.Year).IsRequired();

        builder.Property(c => c.Month).IsRequired();

        builder.Property(c => c.ClosedOn).IsRequired();

        builder.Property(c => c.CreatedAt).IsRequired();

        builder.Ignore(c => c.Competence);

        // Uma competencia so pode ser encerrada uma vez por usuario, e a consulta
        // dominante e "os encerramentos deste usuario nesta faixa de competencias".
        builder.HasIndex(c => new { c.UserId, c.Year, c.Month }).IsUnique();
    }
}
