using Microsoft.AspNetCore.Identity;
using MyApp.Models;

namespace MyApp
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }

            var user = await userManager.FindByEmailAsync("romanovamr0409@gmail.com");
            if (user == null)
            {
                user = new ApplicationUser { UserName = "romanovamr0409@gmail.com", Email = "romanovamr0409@gmail.com" };
                await userManager.CreateAsync(user, "12345_Rm");
            }

            if (!await userManager.IsInRoleAsync(user, "admin"))
            {
                await userManager.AddToRoleAsync(user, "admin");
            }
        }
    }
}
