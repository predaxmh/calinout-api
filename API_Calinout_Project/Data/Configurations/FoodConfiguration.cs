using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class FoodConfiguration : IEntityTypeConfiguration<Food>
    {
        public void Configure(EntityTypeBuilder<Food> builder)
        {
            builder.ToTable("Foods");
            builder.HasKey(f => f.Id);

            builder.Property(f => f.UserId)
                   .IsRequired();

            builder.Property(f => f.FoodTypeId)
                   .IsRequired();

            builder.Property(f => f.MealId)
                .IsRequired(false);

            builder.Property(f => f.Name)
                    .IsRequired()
                    .HasMaxLength(200);

            builder.Property(f => f.WeightInGrams)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(f => f.IsTemplate)
                .IsRequired();

            builder.Property(f => f.ConsumedAt)
                   .IsRequired(false);

            // snapshot fields
            builder.Property(f => f.Calories)
                .HasPrecision(7, 2)
                .IsRequired();

            builder.Property(f => f.Protein)
                .HasPrecision(7, 2)
                .IsRequired();

            builder.Property(f => f.Fat)
                .HasPrecision(7, 2)
                .IsRequired();

            builder.Property(f => f.Carbs)
                .HasPrecision(7, 2)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(f => f.Meal)
                   .WithMany(m => m.Foods)
                   .HasForeignKey(f => f.MealId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.FoodType)
                   .WithMany(ft => ft.Foods)
                   .HasForeignKey(f => f.FoodTypeId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.User)
                    .WithMany(u => u.Foods)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(f => new { f.UserId, f.ConsumedAt });
        }
    }
}