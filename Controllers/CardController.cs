
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepetitionSchedulerService _scheduler;
        private readonly ILogger<CardController> _logger;
        private readonly IGamificationService _gamificationService;

        public CardController(
            ApplicationDbContext context,
            ILogger<CardController> logger,
            IRepetitionSchedulerService scheduler,
            IGamificationService gamificationService)  
        {
            _context = context;
            _logger = logger;
            _scheduler = scheduler;
            _gamificationService = gamificationService;
        }


        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var savedQuestions = await _context.SavedQuestions
                .Include(sq => sq.Question)
                    .ThenInclude(q => q.AnswerOptions)
                .Where(sq => sq.UserId == userId && sq.Question != null)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var masteredCards = savedQuestions.Where(sq => sq.EF >= 4).ToList();
            var nonMasteredCards = savedQuestions.Except(masteredCards).ToList();
            var newCards = nonMasteredCards.Where(sq => sq.NextReview == null).ToList();
            var overdueCards = nonMasteredCards.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value < today).ToList();
            var todayCards = nonMasteredCards.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value == today).ToList();
            var tomorrowCards = nonMasteredCards.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value == tomorrow).ToList();

            var futureCards = nonMasteredCards
                .Where(sq => sq.NextReview.HasValue && sq.NextReview.Value > tomorrow)
                .GroupBy(sq => sq.NextReview.Value)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.MasteredCards = masteredCards;
            ViewBag.NewCards = newCards;
            ViewBag.OverdueCards = overdueCards;
            ViewBag.TodayCards = todayCards;
            ViewBag.TomorrowCards = tomorrowCards;
            ViewBag.FutureCards = futureCards;
            ViewBag.Today = today;
            ViewBag.Tomorrow = tomorrow;

            // 🎯 Гейміфікація
            var gamification = await _gamificationService.GetOrCreateUserGamificationAsync(userId);

            // ✨ Додаємо бейджі за кількість збережених карток
            if (savedQuestions.Count >= 5)
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🔥",
                    title: "Початок шляху",
                    description: "Збережено 5 карток");
            }

            if (savedQuestions.Count >= 15)
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🚀",
                    title: "Серйозний намір",
                    description: "Збережено 15 карток");
            }

            if (savedQuestions.Count >= 50)
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🧠",
                    title: "Карточковий майстер",
                    description: "Збережено 50 карток");
            }

            if (savedQuestions.Count >= 100)
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🏆",
                    title: "Чемпіон пам’яті",
                    description: "Збережено 100 карток");
            }

            // ✨ Бейджі за прогрес
            if (masteredCards.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🌱",
                    title: "Перша опанована",
                    description: "Опановано першу картку");
            }

            if (overdueCards.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "⏰",
                    title: "Перша прострочена",
                    description: "Перше прострочене повторення");
            }

            if (todayCards.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "📆",
                    title: "Перше повторення",
                    description: "Перша картка для повторення сьогодні");
            }

            if (newCards.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    icon: "🆕",
                    title: "Перша нова картка",
                    description: "Перша нова картка у навчанні");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateAnswer([FromBody] AnswerRatingModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var saved = await _context.SavedQuestions
                .FirstOrDefaultAsync(q => q.UserId == userId && q.QuestionId == model.QuestionId);

            if (saved == null) return NotFound();

            var tomorrow = DateTime.UtcNow.AddDays(1).Date;
            var today = DateTime.UtcNow.Date;

            if (saved.NextReview == null || saved.NextReview < tomorrow)
            {
                if (model.Quality < 0 || model.Quality > 5)
                    return BadRequest("Недопустима оцінка");

                var result = _scheduler.CalculateNext(
                    today,
                    model.Quality,
                    saved.Repetition,
                    saved.Interval,
                    saved.EF
                );

                saved.Repetition = result.Repetition;
                saved.Interval = result.Interval;
                saved.EF = result.EFactor;
                saved.NextReview = result.NextReview;

                await _context.SaveChangesAsync();
                // Оновлюємо бейджі після оновлення рейтингу
                var gamification = await _gamificationService.GetOrCreateUserGamificationAsync(userId);
                await _gamificationService.UpdateStreakAsync(userId);

            }

            return Ok();
        }
        public IActionResult GetFullCard(int id)
        {
            _logger.LogInformation("GetFullCard called with id: {Id}", id);

            var question = _context.SavedQuestions
                .Include(sq => sq.Question)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefault(q => q.QuestionId == id);

            if (question == null)
            {
                _logger.LogWarning("No question found with id: {Id}", id);
                return NotFound();
            }

            _logger.LogInformation("Loaded question with id: {Id}, question text: {Text}, answer count: {Count}",
                question.Id,
                question.Question?.Text,
                question.Question?.AnswerOptions?.Count ?? 0);

            return PartialView("_FullCardPartial", question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveCard([FromBody] MoveRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sq = await _context.SavedQuestions.FirstOrDefaultAsync(q => q.QuestionId == model.Id && q.UserId == userId);
            if (sq == null) return NotFound();

            if (DateTime.TryParse(model.NewDate, out DateTime parsedDate))
            {
                sq.NextReview = parsedDate.Date;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }


        [HttpPost]
        public IActionResult DeleteQuestion(int questionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var question = _context.SavedQuestions.FirstOrDefault(q => q.QuestionId == questionId && q.UserId == userId);

            if (question == null)
                return NotFound();

            _context.SavedQuestions.Remove(question);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }

    public class AnswerRatingModel
    {
        public int QuestionId { get; set; }
        public int Quality { get; set; }
    }
    public class MoveRequest
    {
        public int Id { get; set; }
        public string NewDate { get; set; }
    }
}

