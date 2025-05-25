using MyApp.Models;
using MyApp.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Services
{
    public class GamificationService : IGamificationService
    {
        private readonly ApplicationDbContext _context;

        public GamificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserGamification> GetOrCreateUserGamificationAsync(string userId, ApplicationUser user = null)
        {
            var gamification = await _context.UserGamifications
                .Include(g => g.Badges)
                .FirstOrDefaultAsync(g => g.UserId == userId);

            if (gamification == null)
            {
                gamification = new UserGamification
                {
                    UserId = userId,
                    Streak = 1,
                    LastActivityDate = DateTime.Today,
                    User = user
                };
                _context.UserGamifications.Add(gamification);
                await _context.SaveChangesAsync();
            }

            return gamification;
        }

        public async Task UpdateStreakAsync(string userId)
        {
            var gamification = await _context.UserGamifications.FirstOrDefaultAsync(g => g.UserId == userId);

            if (gamification != null)
            {
                var last = gamification.LastActivityDate?.Date;
                if (last == DateTime.Today.AddDays(-1))
                {
                    gamification.Streak += 1;
                }
                else if (last != DateTime.Today)
                {
                    gamification.Streak = 1;
                }

                gamification.LastActivityDate = DateTime.Today;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddBadgeIfNotExistsAsync(UserGamification gamification,  string icon, string title, string description)
        {
            if ( !gamification.Badges.Any(b => b.Title == title))
            {
                gamification.Badges.Add(new UserBadge
                {
                    UserId = gamification.UserId,
                    Title = title,
                    Description = description,
                    EmojiIcon = icon,
                    DateEarned = DateTime.Today
                });

                await _context.SaveChangesAsync();
            }
        }
        public string GetMotivationalMessage(int streak) => streak switch
        {
            >= 30 => "🎉 Вау! Місяць навчання без зупинок! Ти молодець!",
            >= 14 => "🚀 Чудова робота! 14 днів поспіль!",
            >= 7 => "🏆 Ти легенда! Тиждень без зупинок 🚀",
            >= 3 => "🔥 Продовжуй в тому ж дусі!",
            >= 1 => "🌱 Молодець! Твій шлях тільки починається!",
            _ => "Час почати навчання!"
        };

        public List<BadgeProgress> GetInProgressBadges(UserGamification gamification)
        {
            var list = new List<BadgeProgress>();

            var badgeThresholds = new List<(int Threshold, string Title, string Description, string Emoji)>
        {
            (1, "Початок навчання", "Перший день навчання! Молодець!", "🌱"),
            (3, "3 дні поспіль", "Навчаєшся щодня протягом 3 днів", "🔥"),
            (7, "7 днів поспіль", "Тиждень без пропусків!", "🏆"),
            (14, "14 днів поспіль", "Два тижні наполегливості!", "🚀"),
            (30, "30 днів поспіль", "Місяць без зупинок! Справжній чемпіон!", "🎯"),
        };

            foreach (var badge in badgeThresholds)
            {
                if (!gamification.Badges.Any(b => b.Title == badge.Title))
                {
                    list.Add(new BadgeProgress
                    {
                        Title = badge.Title,
                        Description = badge.Description,
                        EmojiIcon = badge.Emoji,
                        ProgressPercentage = Math.Min(gamification.Streak / (double)badge.Threshold * 100, 100)
                    });
                }
            }

            return list;
        }
    }
}
