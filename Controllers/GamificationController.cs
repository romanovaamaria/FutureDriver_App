using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using MyApp.Services;
using MyApp.ViewModels;

namespace MyApp.Controllers
{
    [Authorize]
    public class GamificationController : Controller
    {
        private readonly IGamificationService _gamificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public GamificationController(IGamificationService gamificationService, UserManager<ApplicationUser> userManager)
        {
            _gamificationService = gamificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var gamification = await _gamificationService.GetOrCreateUserGamificationAsync(user.Id, user);

            await _gamificationService.UpdateStreakAsync(user.Id);

            // Додаємо бейджі
            var badgesToAdd = new List<(string Icon, string Title, string Description, int Threshold)>
    {
        ("🌱", "Початок навчання", "Перший день навчання! Молодець!", 1),
        ("🔥", "3 дні поспіль", "Навчаєшся щодня протягом 3 днів", 3),
        ("🏆", "7 днів поспіль", "Тиждень без пропусків!", 7),
        ("🚀", "14 днів поспіль", "Два тижні наполегливості!", 14),
        ("🎯", "30 днів поспіль", "Місяць без зупинок! Справжній чемпіон!", 30),
    };

            foreach (var badge in badgesToAdd)
            {
                // Перевірка, чи ще немає бейджа та чи досягнуто порогу
                if (!gamification.Badges.Any(b => b.Title == badge.Title) && gamification.Streak >= badge.Threshold)
                {
                    await _gamificationService.AddBadgeIfNotExistsAsync(gamification, badge.Icon, badge.Title, badge.Description);
                }
            }
            var viewModel = new GamificationViewModel
            {
                StreakDays = gamification.Streak,
                MotivationalMessage = _gamificationService.GetMotivationalMessage(gamification.Streak),
                EarnedBadges = gamification.Badges.OrderByDescending(b => b.DateEarned).ToList(),
                InProgressBadges = _gamificationService.GetInProgressBadges(gamification)
            };

            return View(viewModel);
        }
    }
}
