using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    /*public class CustomCard
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Питання")]
        public string Question { get; set; }

        [Required]
        [Display(Name = "Відповідь")]
        public string Answer { get; set; }

        [Display(Name = "Зображення")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Категорія")]
        public string Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Поля для інтервального повторення
        public int RepetitionLevel { get; set; } = 0; // Рівень повторення
        public DateTime? NextRepetitionDate { get; set; } // Дата наступного повторення
        public DateTime? LastReviewedAt { get; set; } // Коли востаннє переглядалась
    }*/
    public class CustomCard
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string AnswerText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public ICollection<SavedQuestion> SavedQuestions { get; set; } = new List<SavedQuestion>();
    }
}

