using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using MyApp.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace MyApp.Controllers
{
    [Authorize]
    public class TestResultsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string category = null, DateTime? from = null, DateTime? to = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var resultsQuery = _context.TestResults
                .Where(r => r.UserId == userId);

            if (!string.IsNullOrEmpty(category))
                resultsQuery = resultsQuery.Where(r => r.Category == category);

            if (from.HasValue)
                resultsQuery = resultsQuery.Where(r => r.DateTaken >= from.Value);

            if (to.HasValue)
                resultsQuery = resultsQuery.Where(r => r.DateTaken <= to.Value);

            var categories = _context.TestResults
                .Where(r => r.UserId == userId)
                .Select(r => r.Category)
                .Distinct()
                .ToList();

            ViewBag.Categories = categories;

            var results = resultsQuery
                .OrderBy(r => r.DateTaken)
                .ToList();

            return View(results);
        }
        public IActionResult Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = _context.TestResults
                .FirstOrDefault(r => r.Id == id && r.UserId == userId);

            if (result == null)
            {
                return NotFound();
            }

            // Десеріалізуємо мінімальний summary
            var questionSummaries = JsonSerializer.Deserialize<List<QuestionSummaryDto>>(result.QuestionsJson);

            // Отримуємо повні питання з БД за їх Id
            var questionIds = questionSummaries.Select(q => q.QuestionId).ToList();

            var questions = _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => questionIds.Contains(q.Id))
                .ToList();

            // Формуємо розгорнутий список питань із результатами
            var questionResults = questionSummaries.Select(qs =>
            {
                var question = questions.FirstOrDefault(q => q.Id == qs.QuestionId);

                return new QuestionResultJson
                {
                    QuestionId = qs.QuestionId,
                    QuestionText = question?.Text ?? "Питання не знайдено",
                    AnswerOptions = question?.AnswerOptions
                        .Select(a => new AnswerOptionDto
                        {
                            Id = a.Id,
                            Text = a.Text,
                            IsCorrect = a.IsCorrect
                        })
                        .ToList() ?? new List<AnswerOptionDto>(),
                    SelectedAnswerId = qs.SelectedAnswerId,
                    IsCorrect = qs.IsCorrect
                };
            }).ToList();

            var viewModel = new TestResultDetailsViewModel
            {
                TestResult = result,
                QuestionResults = questionResults
            };

            return View(viewModel);
        }
    }
}
