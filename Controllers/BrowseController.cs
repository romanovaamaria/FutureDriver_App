using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class BrowseController: Controller   
    {
        private readonly ApplicationDbContext _context;

        public BrowseController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(string query)
        {
            var results = _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => q.Text.Contains(query) || q.Category.Contains(query))
                .ToList();
            return View(results);
        }

    }
}
