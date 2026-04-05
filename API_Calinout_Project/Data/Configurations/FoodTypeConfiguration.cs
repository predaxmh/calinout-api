using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class FoodTypeConfiguration : IEntityTypeConfiguration<FoodType>
    {
        public void Configure(EntityTypeBuilder<FoodType> builder)
        {
            builder.ToTable("FoodTypes");
            builder.HasKey(ft => ft.Id);

            builder.Property(ft => ft.UserId)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(ft => ft.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(ft => ft.Calories)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(ft => ft.Protein)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(ft => ft.Fat)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(ft => ft.Carbs)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(ft => ft.BaseWeightInGrams)
                   .HasPrecision(7, 2)
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                    .IsRequired();

            builder.Property(r => r.UpdatedAt)
                    .IsRequired(false);

            builder.HasIndex(ft => ft.Name)
                    .IsUnique();

        }
    }

}