using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyApp.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Category { get; set; }
        public string? ImageUrl { get; set; }
        [NotMapped]
        //[FileExtensions(Extensions = "jpg,jpeg,png,gif", ErrorMessage = "Please upload a valid image file (jpg, jpeg, png, gif).")]
        public IFormFile? ImageFile { get; set; }
        public List<AnswerOption> AnswerOptions { get; set; }
        public int? SelectedAnswerId { get; set; } //властивість для зберігання вибраної відповіді
    }

    public class AnswerOption
    {
        public int Id { get; set; } //айді в базі даних
        public int Number { get; set; } //номер в межах одного питання
        public string Text { get; set; }
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }
        public Question? Question { get; set; }
    }
    /* public class SavedQuestion
     {
         public int Id { get; set; }
         public string UserId { get; set; }
         public int QuestionId { get; set; }
     }*/

    public class SavedQuestion
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        public int? QuestionId { get; set; }
        public Question? Question { get; set; }

        public int? CustomCardId { get; set; }
        public CustomCard? CustomCard { get; set; }

        public DateTime? NextReview { get; set; }
        public int? Repetition { get; set; }
        public int? Interval { get; set; }
        public double? EF { get; set; } = 2.5; // Easiness factor
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
