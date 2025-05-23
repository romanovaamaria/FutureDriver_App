using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
        public IActionResult StartTest(string format, string category = null)
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
                _questions = _context.Questions
                    .Include(q => q.AnswerOptions)
                    .OrderBy(r => EF.Functions.Random())
                    .Take(20)
                    .ToList();
            }
            else
            {
                // Handle invalid format
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
            if (action == "Next")
            {
                _selectedAnswers.Add(selectedAnswer);
                _currentQuestionIndex++;
                if (_currentQuestionIndex == _questions.Count - 1)
                {
                    ViewBag.IsLastQuestion = true;
                }

                if (_currentQuestionIndex < _questions.Count)
                {
                    return View("Index", _questions[_currentQuestionIndex]);
                }
                else
                {
                    return FinishTest(startTime);
                }
            }
            else if (action == "Finish")
            {
                return FinishTest(startTime);
            }
            return View("Index", _questions[_currentQuestionIndex]);
        }

        private IActionResult FinishTest(long startTime)
        {
            var results = EvaluateResults();
            float percentage = (correctAnswersCount / (float)_questions.Count) * 100;
            ViewBag.CorrectAnswersCount = correctAnswersCount;
            ViewBag.TotalQuestions = _questions.Count;
            ViewBag.Percentage = Math.Round(percentage, 2);

            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var totalTimeSpent = (endTime - startTime) / 1000;
            ViewBag.TotalTimeSpent = totalTimeSpent;

            TempData["ClearLocalStorage"] = true;

            return View("Result", results);
        }

        [HttpPost]
        public IActionResult SaveQuestion(IFormCollection form)
        {
            var questionId = form["questionId"];
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingSavedQuestion = _context.SavedQuestions
                .FirstOrDefault(sq => sq.UserId == userId && sq.QuestionId == int.Parse(questionId));

            if (existingSavedQuestion != null)
            {
                return Ok();
            }
            var savedQuestion = new SavedQuestion
            {
                UserId = userId,
                QuestionId = int.Parse(questionId)
            };
            _context.SavedQuestions.Add(savedQuestion);
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
                    if (index >= 1)
                    {
                        if (_questions[i].AnswerOptions[index - 1].IsCorrect == true)
                        {
                            correctAnswersCount++;
                        }
                    }

                }
            }
            return _questions;
        }
    }
}
