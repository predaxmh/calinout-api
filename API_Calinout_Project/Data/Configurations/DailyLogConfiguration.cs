using API_Calinout_Project.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API_Calinout_Project.Data.Configurations
{
    public class DailyLogConfiguration : IEntityTypeConfiguration<DailyLog>
    {
        public void Configure(EntityTypeBuilder<DailyLog> builder)
        {
            builder.ToTable("DailyLogs");
            builder.HasKey(dl => dl.Id);

            builder.Property(dl => dl.UserId).IsRequired();
            builder.Property(dl => dl.Date).IsRequired();

            // Basic scalar properties
            builder.Property(dl => dl.BurnedCalories).
                IsRequired(false);

            builder.Property(dl => dl.WeightAtLog)
                .HasPrecision(5, 2)
                .IsRequired(false);

            builder.Property(dl => dl.DigestiveTrackCleared)
                .IsRequired();

            builder.Property(dl => dl.IsCheatDay)
                .IsRequired();

            builder.Property(dl => dl.TargetCalorieOnThisDay)
                .IsRequired(false);

            builder.Property(dl => dl.TotalCalories)
                .IsRequired(false);

            builder.Property(dl => dl.TotalProtein)
                .HasPrecision(7, 2)
                .IsRequired(false);

            builder.Property(dl => dl.TotalCarbs)
                .HasPrecision(7, 2)
                .IsRequired(false);

            builder.Property(dl => dl.TotalFat)
                .HasPrecision(7, 2)
                .IsRequired(false);

            builder.Property(dl => dl.TotalFoodWeight)
                .HasPrecision(7, 2)
                .IsRequired(false);

            builder.Property(dl => dl.DailyNotes)
                .HasMaxLength(2000);

            builder.Property(r => r.CreatedAt)
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(d => d.User)
                .WithMany(a => a.DailyLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(dl => new { dl.UserId, dl.Date })
                    .IsUnique();
        }
    }
}