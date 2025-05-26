
using System.ComponentModel.DataAnnotations.Schema;
namespace MyApp.Models
    {


        public class TestResult
        {
            public int Id { get; set; }
            public string UserId { get; set; }

            public DateTime DateTaken { get; set; } = DateTime.UtcNow;

            public string? Category { get; set; }

            public int CorrectAnswers { get; set; }
            public int TotalQuestions { get; set; }
            public float Percentage { get; set; } // 0.0 - 100.0

            public int TimeSpentSeconds { get; set; } // Тривалість проходження

            public string QuestionsJson { get; set; } = "";

        }
    }


