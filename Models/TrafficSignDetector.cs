using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;

namespace MyApp.Models
{
    public class TrafficSignDetector
    {
        private readonly string _batScriptPath;
        private readonly string _modelPath;
        private readonly ILogger<TrafficSignDetector> _logger;

        public TrafficSignDetector(string basePath, ILogger<TrafficSignDetector> logger)
        {
            _logger = logger;

            // Use relative paths based on the application's base path
            _batScriptPath = Path.Combine(basePath, "PythonEnv", "run_detector.bat");
            _modelPath = Path.Combine(basePath, "PythonEnv", "models", "model.h5");

            // Check if files exist
            if (!File.Exists(_batScriptPath))
            {
                _logger.LogError($"Batch file not found: {_batScriptPath}");
                throw new FileNotFoundException("Python batch file not found", _batScriptPath);
            }

            if (!File.Exists(_modelPath))
            {
                _logger.LogError($"Model not found: {_modelPath}");
                throw new FileNotFoundException("Model file not found", _modelPath);
            }
        }

        public async Task<string> DetectSignAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogError($"Image not found: {imagePath}");
                throw new FileNotFoundException("Image not found", imagePath);
            }

            using (var process = new Process())
            {
                process.StartInfo.FileName = _batScriptPath;
                process.StartInfo.Arguments = $"\"{imagePath}\" \"{_modelPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                _logger.LogInformation($"Starting process: {_batScriptPath} {process.StartInfo.Arguments}");

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await Task.Run(() => process.WaitForExit());

                // Запис stderr як INFO, якщо немає слова "error"
                if (!string.IsNullOrWhiteSpace(error))
                {
                    if (error.ToLower().Contains("error") || error.ToLower().Contains("exception"))
                    {
                        _logger.LogError($"Python stderr: {error}");
                        throw new Exception($"Python error: {error}");
                    }
                    else
                    {
                        _logger.LogInformation($"Python stderr (non-critical): {error}");
                    }
                }

                string cleanedOutput = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line =>
                !line.Contains("tensorflow", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("ETA:") &&
                !line.Contains("step") &&
                !line.Contains("=") &&
                !line.Contains("Creating new thread pool"))
            .LastOrDefault()?.Trim();

                if (string.IsNullOrWhiteSpace(cleanedOutput))
                {
                    _logger.LogWarning("Could not extract detection result from Python output.");
                    throw new Exception("Не вдалося виділити результат розпізнавання.");
                }
                string outputWithClassName = GetClassNameFromString(cleanedOutput);
                _logger.LogInformation($"Результат розпізнавання: {outputWithClassName}");
                return outputWithClassName;
            }
        }

        private static readonly Dictionary<int, string> ClassNames = new Dictionary<int, string>()
    {
        {0, "Обмеження швидкості 20 км/год"},
        {1, "Обмеження швидкості 30 км/год"},
        {2, "Обмеження швидкості 50 км/год"},
        {3, "Обмеження швидкості 60 км/год"},
        {4, "Обмеження швидкості 70 км/год"},
        {5, "Обмеження швидкості 80 км/год"},
        {6, "Кінець обмеження швидкості 80 км/год"},
        {7, "Обмеження швидкості 100 км/год"},
        {8, "Обмеження швидкості 120 км/год"},
        {9, "Обгін заборонено"},
        {10, "Обгін заборонено для вантажівок понад 3.5 т"},
        {11, "Головна дорога"},
        {12, "Головна дорога"},
        {13, "Увага! Дати дорогу"},
        {14, "Стоп"},
        {15, "Рух заборонено"},
        {16, "Рух вантажівок понад 3.5 т заборонено"},
        {17, "В’їзд заборонено"},
        {18, "Загальна небезпека"},
        {19, "Небезпечний поворот ліворуч"},
        {20, "Небезпечний поворот праворуч"},
        {21, "Подвійний поворот"},
        {22, "Нерівна дорога"},
        {23, "Слизька дорога"},
        {24, "Дорога звужується справа"},
        {25, "Дорожні роботи"},
        {26, "Світлофорний об’єкт"},
        {27, "Пішоходи"},
        {28, "Діти"},
        {29, "Велосипедна доріжка"},
        {30, "Обережно, слизько"},
        {31, "Перетинання з дикими тваринами"},
        {32, "Кінець усіх обмежень"},
        {33, "Поворот праворуч"},
        {34, "Поворот ліворуч"},
        {35, "Прямо лише"},
        {36, "Прямо або праворуч"},
        {37, "Прямо або ліворуч"},
        {38, "Тримайтеся правого краю"},
        {39, "Тримайтеся лівого краю"},
        {40, "Круговий рух"},
        {41, "Кінець заборони обгону"},
        {42, "Кінець заборони обгону вантажівок понад 3.5 т"}
    };

        public static string GetClassNameFromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            // Витягнути число з рядка, наприклад "[1]" -> 1
            var match = Regex.Match(input, @"\[(\d+)\]");
            if (!match.Success)
                return null;

            if (int.TryParse(match.Groups[1].Value, out int classNo))
            {
                if (ClassNames.TryGetValue(classNo, out string className))
                    return className;
            }

            return null;
        }
    }
}

