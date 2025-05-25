using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class UserGamification
    {
        [Key]
        public string UserId { get; set; }
        public int Streak { get; set; }
        public DateTime? LastActivityDate { get; set; }

        public ICollection<UserBadge> Badges { get; set; }
        public ApplicationUser User { get; set; }
    }
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EmojiIcon { get; set; }
        public DateTime DateEarned { get; set; }
        public double? ProgressPercentage { get; set; }  

        public UserGamification Gamification { get; set; }
    }
}
