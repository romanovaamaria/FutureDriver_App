namespace MyApp.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace MyApp.Models
    {
        public class TestResult
        {
            [Key]
            public int Id { get; set; }

            public string? UserId { get; set; }

            public string Category { get; set; }

            public int CorrectAnswers { get; set; }

            public int TotalQuestions { get; set; }

            public float Percentage { get; set; }

            public int TimeSpentSeconds { get; set; }

            public DateTime TestDate { get; set; }
        }
    }
}
