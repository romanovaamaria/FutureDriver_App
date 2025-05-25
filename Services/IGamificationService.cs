using MyApp.Models;
using MyApp.ViewModels;

namespace MyApp.Services
{
    public interface IGamificationService
    {
        Task<UserGamification> GetOrCreateUserGamificationAsync(string userId, ApplicationUser user = null);
        Task AddBadgeIfNotExistsAsync(UserGamification gamification, string icon, string title, string description);
        Task UpdateStreakAsync(string userId);
        string GetMotivationalMessage(int streak);
        List<BadgeProgress> GetInProgressBadges(UserGamification gamification);
    }
}
