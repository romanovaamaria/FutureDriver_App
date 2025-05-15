using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        public string Nickname { get; set; }

        public string ProfilePicturePath { get; set; }

        public int StreakCount { get; set; }

        public DateTime LastActiveDate { get; set; }

        // Додаткові поля
        public int TotalTestsTaken { get; set; }

        public int TotalCorrectAnswers { get; set; }

        public int TotalAchievements { get; set; }
    }
}
