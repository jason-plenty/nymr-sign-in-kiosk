using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NymrSignIn.Domain.Register;

namespace NymrSignIn.Infrastructure.Persistence.Register;

public sealed class SiteRegisterEntryConfiguration : IEntityTypeConfiguration<SiteRegisterEntry>
{
    public void Configure(EntityTypeBuilder<SiteRegisterEntry> builder)
    {
        builder.ToTable("SiteRegisterEntries", "kiosk");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Organisation)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.SignatureUrl)
            .HasMaxLength(2048);

        builder.Property(e => e.DateIn)
            .HasColumnType("date");

        builder.Property(e => e.TimeIn)
            .HasColumnType("time");

        builder.Property(e => e.TimeOut)
            .HasColumnType("time");

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.MedicalStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.AdditionalInfo)
            .HasMaxLength(2000);

        builder.Property(e => e.SiteCode)
            .HasMaxLength(32);

        builder.Property(e => e.SiteCodeGenerated)
            .HasMaxLength(32);

        builder.HasIndex(e => e.DateIn);
        builder.HasIndex(e => new { e.DateIn, e.Status });

        builder.Ignore(e => e.IsSignedOut);
    }
}
