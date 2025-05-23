
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Controllers
{
    [Authorize]
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardController> _logger;

        public CardController(ApplicationDbContext context, ILogger<CardController> logger)
        {
            _context = context;
            _logger = logger;
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

            // Групуємо питання за категоріями
            var newCards = savedQuestions.Where(sq => sq.NextReview == null).ToList();
            var overdueCards = savedQuestions.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value < today).ToList();
            var todayCards = savedQuestions.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value == today).ToList();
            var tomorrowCards = savedQuestions.Where(sq => sq.NextReview.HasValue && sq.NextReview.Value == tomorrow).ToList();

            // Групуємо майбутні дати
            var futureCards = savedQuestions
                .Where(sq => sq.NextReview.HasValue && sq.NextReview.Value > tomorrow)
                .GroupBy(sq => sq.NextReview.Value)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Передаємо дані через ViewBag
            ViewBag.NewCards = newCards;
            ViewBag.OverdueCards = overdueCards;
            ViewBag.TodayCards = todayCards;
            ViewBag.TomorrowCards = tomorrowCards;
            ViewBag.FutureCards = futureCards;
            ViewBag.Today = today;
            ViewBag.Tomorrow = tomorrow;

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

            // Дозволяємо зміну лише якщо дата сьогодні або в минулому
            if (saved.NextReview == null || saved.NextReview < tomorrow)
            {
                int quality = model.Quality;
                if (quality < 0 || quality > 5)
                    return BadRequest("Недопустима оцінка");

                saved.Repetition ??= 0;
                saved.Interval ??= 1;
                saved.EF ??= 2.5;

                if (quality >= 3)
                {
                    if (saved.Repetition == 0)
                        saved.Interval = 1;
                    else if (saved.Repetition == 1)
                        saved.Interval = 6;
                    else
                        saved.Interval = (int)Math.Round(saved.Interval.Value * saved.EF.Value);

                    saved.Repetition += 1;
                }
                else
                {
                    saved.Repetition = 0;
                    saved.Interval = 1;
                }

                saved.EF += (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
                if (saved.EF < 1.3)
                    saved.EF = 1.3;

                saved.NextReview = today.AddDays(saved.Interval.Value);
                await _context.SaveChangesAsync();
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

