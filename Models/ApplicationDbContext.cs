using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using System.Reflection.Emit;

namespace MyApp.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<SavedQuestion> SavedQuestions { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<CustomCard> CustomCards { get; set; }
        public DbSet<UserGamification> UserGamifications { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<QuestionStatistics> QuestionStatistics { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<CalendarTask> CalendarTasks { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Визначення зв’язку один-до-багатьох між Question та AnswerOption
            builder.Entity<AnswerOption>()
                .HasOne(a => a.Question)
                .WithMany(q => q.AnswerOptions)
                .HasForeignKey(a => a.QuestionId);

            builder.Entity<UserGamification>()
                .HasOne(g => g.User)
                .WithOne()
                .HasForeignKey<UserGamification>(g => g.UserId);

            builder.Entity<UserBadge>()
                .HasOne(b => b.Gamification)
                .WithMany(g => g.Badges)
                .HasForeignKey(b => b.UserId);
            // Композиція: при видаленні UserSettings видаляються CalendarTasks
            builder.Entity<UserSettings>()
                .HasMany(us => us.CalendarTasks)
                .WithOne(ct => ct.UserSettings)
                .HasForeignKey(ct => ct.UserSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
