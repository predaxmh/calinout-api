using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class MealConfiguration : IEntityTypeConfiguration<Meal>
    {
        public void Configure(EntityTypeBuilder<Meal> builder)
        {
            builder.ToTable("Meals");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.UserId)
                   .IsRequired();

            builder.Property(m => m.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(m => m.IsTemplate)
                   .IsRequired().
                   HasDefaultValue(true);

            builder.Property(m => m.ConsumedAt)
                .IsRequired(false);


            // Totals (denormalized)
            builder.Property(m => m.TotalCalories)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(m => m.TotalProtein)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(m => m.TotalCarbs)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(m => m.TotalFat)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(m => m.TotalWeight)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                    .IsRequired();

            builder.Property(r => r.UpdatedAt)
                    .IsRequired(false);

            builder.HasOne(f => f.User)
                   .WithMany(u => u.Meals)
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}