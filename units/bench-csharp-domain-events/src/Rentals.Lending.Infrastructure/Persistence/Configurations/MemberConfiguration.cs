using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.Lending.Domain.Members;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasConversion(id => id.Value, value => new MemberId(value));
        builder.Property(m => m.FullName).HasMaxLength(200);
        builder.Property(m => m.Email).HasMaxLength(320);
        builder.Property(m => m.Phone).HasMaxLength(40);
        builder.Property(m => m.SuspensionReason).HasMaxLength(500);
        builder.Ignore(m => m.DomainEvents);
    }
}
