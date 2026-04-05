using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Table Name (Optional, but good practice)
            builder.ToTable("RefreshTokens");

            // Primary Key
            builder.HasKey(x => x.Id);

            // Property Configurations (Replaces Attributes)
            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.CreatedByIp)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.RevokedByIp)
                .HasMaxLength(64);

            // Relationships
            builder.HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, tokens are gone
        }
    }
}