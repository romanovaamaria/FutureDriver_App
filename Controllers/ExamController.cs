using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using MyApp.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;

namespace MyApp.Controllers
{
    public class ExamController : Controller
    {
        private static List<Question> _questions;
        private static int _currentQuestionIndex = 0;
        private static List<int> _selectedAnswers = new List<int>();
        private static int correctAnswersCount = 0;
        private readonly ApplicationDbContext _context;

        public ExamController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult SelectTestFormat()
        {
            var categories = _context.Questions
                .Select(q => q.Category)
                .Distinct()
                .ToList();

            ViewBag.Categories = categories;
            return View();
        }
        [HttpPost]
        public IActionResult StartTest(string format, string category = null, int? testResultId = null, int? randomCount = null)
        {
            if (format == "category" && !string.IsNullOrEmpty(category))
            {
                _questions = _context.Questions
                    .Include(q => q.AnswerOptions)
                    .Where(q => q.Category == category)
                    .ToList();
            }
            else if (format == "random")
            {
                int totalQuestions = _context.Questions.Count();
                if (!randomCount.HasValue || randomCount.Value < 1 || randomCount.Value > totalQuestions)
                {
                    TempData["Error"] = $"Кількість випадкових питань повинна бути від 1 до {totalQuestions}.";
                    return RedirectToAction("SelectTestFormat");
                }

                _questions = _context.Questions
                    .Include(q => q.AnswerOptions)
                    .OrderBy(r => EF.Functions.Random())
                    .Take(randomCount.Value)
                    .ToList();
            }
            else if (format == "previous" && testResultId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = _context.TestResults
                    .FirstOrDefault(r => r.Id == testResultId.Value && r.UserId == userId);

                if (result == null)
                {
                    return NotFound("Test result not found.");
                }

                var questionSummaries = JsonSerializer.Deserialize<List<QuestionSummaryDto>>(result.QuestionsJson);
                var questionIds = questionSummaries.Select(q => q.QuestionId).ToList();

                _questions = _context.Questions
                    .Include(q => q.AnswerOptions)
                    .Where(q => questionIds.Contains(q.Id))
                    .ToList();
            }
            else
            {
                return BadRequest("Invalid test format selected.");
            }

            _currentQuestionIndex = 0;
            correctAnswersCount = 0;
            ViewBag.IsLastQuestion = false;
            _selectedAnswers.Clear();
            TempData["ClearLocalStorage"] = true;

            return View("Index", _questions[_currentQuestionIndex]);
        }

        public IActionResult Index()
        {
            return RedirectToAction("SelectTestFormat");
        }

        [HttpPost]
        public IActionResult SubmitAnswer(int selectedAnswer, string action, long startTime)
        {
            _selectedAnswers.Add(selectedAnswer);
            _currentQuestionIndex++;

            if (_currentQuestionIndex == _questions.Count - 1)
            {
                ViewBag.IsLastQuestion = true;
            }

            if (action == "Next" && _currentQuestionIndex < _questions.Count)
            {
                return View("Index", _questions[_currentQuestionIndex]);
            }
            else // Finish
            {
                return FinishTest(startTime);
            }
        }

        private IActionResult FinishTest(long startTime)
        {
            var results = EvaluateResults();

            // 📊 Оновлення статистики для кожного питання
            foreach (var question in _questions)
            {
                var stat = _context.QuestionStatistics
                    .FirstOrDefault(qs => qs.QuestionId == question.Id);

                if (stat == null)
                {
                    stat = new QuestionStatistics { QuestionId = question.Id };
                    _context.QuestionStatistics.Add(stat);
                }

                stat.TotalAttempts++;

                var selectedOption = question.SelectedAnswerId;
                if (selectedOption.HasValue)
                {
                    var selectedAnswer = question.AnswerOptions[selectedOption.Value - 1];
                    if (!selectedAnswer.IsCorrect)
                    {
                        stat.WrongAttempts++;
                    }
                }
                else
                {
                    stat.WrongAttempts++;
                }
            }
            _context.SaveChanges();

            // 📊 Розрахунок відсотка
            float percentage = (correctAnswersCount / (float)_questions.Count) * 100;
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var totalTimeSpent = (endTime - startTime) / 1000;

            ViewBag.CorrectAnswersCount = correctAnswersCount;
            ViewBag.TotalQuestions = _questions.Count;
            ViewBag.Percentage = Math.Round(percentage, 2);
            ViewBag.TotalTimeSpent = totalTimeSpent;
            TempData["ClearLocalStorage"] = true;

            // 💾 Зберігаємо результат тільки для авторизованих
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var questionsSummary = _questions.Select(q => new QuestionSummaryDto
                {
                    QuestionId = q.Id,
                    SelectedAnswerId = q.SelectedAnswerId,
                    IsCorrect = q.SelectedAnswerId.HasValue &&
                q.AnswerOptions[q.SelectedAnswerId.Value - 1].IsCorrect
                }).ToList();

                var testResult = new TestResult
                {
                    UserId = userId,
                    DateTaken = DateTime.UtcNow,
                    Percentage = percentage,
                    TotalQuestions = _questions.Count,
                    CorrectAnswers = correctAnswersCount,
                    Category = _questions.First().Category,
                    QuestionsJson = JsonSerializer.Serialize(questionsSummary)
                };

                _context.TestResults.Add(testResult);
                _context.SaveChanges();
            }

            return View("Result", results);
        }

        [HttpPost]
        [Authorize] // ❗️ Зберігати питання можуть тільки авторизовані
        public IActionResult SaveQuestion(IFormCollection form)
        {
            var questionId = form["questionId"];
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = _context.SavedQuestions
                .FirstOrDefault(sq => sq.UserId == userId && sq.QuestionId == int.Parse(questionId));

            if (existing != null)
                return Ok();

            var saved = new SavedQuestion
            {
                UserId = userId,
                QuestionId = int.Parse(questionId)
            };
            _context.SavedQuestions.Add(saved);
            _context.SaveChanges();
            return Ok();
        }

        private List<Question> EvaluateResults()
        {
            for (int i = 0; i < _questions.Count; i++)
            {
                if (i < _selectedAnswers.Count)
                {
                    _questions[i].SelectedAnswerId = _selectedAnswers[i];
                    int index = _questions[i].SelectedAnswerId.HasValue ? _questions[i].SelectedAnswerId.Value : 0;
                    if (index >= 1 && _questions[i].AnswerOptions[index - 1].IsCorrect)
                    {
                        correctAnswersCount++;
                    }
                }
            }
            return _questions;
        }
    }
}
