using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // IdentityUser already configures Id, Email, PasswordHash, etc.
            // We only configure OUR custom properties.

            builder.Property(x => x.FirstName)
                .HasMaxLength(50)
                .IsRequired(false); // Or true, depending on if you require it at registration

            builder.Property(x => x.LastName)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(x => x.Gender)
            .IsRequired(false);


            builder.Property(x => x.MeasurementSystem)
                .HasMaxLength(10) // "Metric" or "Imperial"
                .HasDefaultValue("Metric");

            // 🧠 Precision Configuration
            // decimal(5,2) means: Total 5 digits, 2 decimal places.
            // Max value: 999.99

            builder.Property(x => x.WeightInKg)
                .HasPrecision(5, 2); // e.g. 120.55 kg

            builder.Property(x => x.HeightInCm)
                .HasPrecision(5, 2); // e.g. 188.00 cm

            // Relationships
            // (RefreshToken is already configured in RefreshTokenConfiguration, 
            // but you can mirror it here if you like strict bidirectional config)
        }
    }
}