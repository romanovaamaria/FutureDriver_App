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

        public CardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var savedQuestions = await _context.SavedQuestions
                .Where(sq => sq.UserId == userId)
                .Select(sq => sq.QuestionId)
                .ToListAsync();

            var questions = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => savedQuestions.Contains(q.Id))
                .ToListAsync();

            return View(questions);
        }
        [HttpPost]
        public IActionResult DeleteQuestion(int questionId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Отримати ідентифікатор поточного користувача
                var question = _context.SavedQuestions.FirstOrDefault(q => q.QuestionId == questionId && q.UserId == userId);

                if (question == null)
                {
                    return NotFound();
                }
                _context.SavedQuestions.Remove(question);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Помилка при видаленні питання: {ex.Message}");
            }
        }

    }


}
