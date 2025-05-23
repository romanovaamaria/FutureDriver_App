using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.ViewComponents
{
   /* public class CardsToReviewCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CardsToReviewCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Content("0");
            }

            var count = await _context.CustomCards
                .CountAsync(c => c.UserId == userId && c.NextRepetitionDate <= DateTime.Now);

            return Content(count.ToString());
        }
    }*/
}