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

        builder.HasIndex(e => e.DateIn);

        builder.Ignore(e => e.IsSignedOut);
    }
}
