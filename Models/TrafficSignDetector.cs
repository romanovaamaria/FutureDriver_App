﻿using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyApp.Models
{
    public class TrafficSignInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Purpose { get; set; }
    }

    public class TrafficSignDetector
    {
        private readonly InferenceSession _session;
        private readonly ILogger<TrafficSignDetector> _logger;

        public TrafficSignDetector(string basePath, ILogger<TrafficSignDetector> logger)
        {
            _logger = logger;

            string modelPath = Path.Combine(basePath, "MLModels", "model.onnx");

            if (!File.Exists(modelPath))
            {
                _logger.LogError($"Model file not found: {modelPath}");
                throw new FileNotFoundException("ONNX model file not found", modelPath);
            }

            _session = new InferenceSession(modelPath);
        }

        public async Task<TrafficSignInfo> DetectSignAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogError($"Image not found: {imagePath}");
                throw new FileNotFoundException("Image not found", imagePath);
            }

            try
            {
                float[] inputData = PreprocessImage(imagePath);

                var inputTensor = new DenseTensor<float>(new[] { 1, 32, 32, 1 });
                for (int i = 0; i < inputData.Length; i++)
                    inputTensor.Buffer.Span[i] = inputData[i];

                var inputs = new[] { NamedOnnxValue.CreateFromTensor("input", inputTensor) };

                var results = _session.Run(inputs);
                float[] output = results.First().AsEnumerable<float>().ToArray();

                int predictedClass = Array.IndexOf(output, output.Max());

                if (SignsInfo.TryGetValue(predictedClass, out var signInfo))
                {
                    _logger.LogInformation($"Detected sign class: {predictedClass} - {signInfo.Name}");
                    return signInfo;
                }

                _logger.LogWarning($"Unknown class index: {predictedClass}");
                return new TrafficSignInfo
                {
                    Name = $"Невідомий знак [{predictedClass}]",
                    Description = "Знак не розпізнано",
                    Type = "Невідомий",
                    Purpose = "Немає"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Detection error: {ex.Message}");
                throw;
            }
        }

        private float[] PreprocessImage(string imagePath)
        {
            Bitmap bmp = new Bitmap(imagePath);
            Bitmap resized = new Bitmap(bmp, new Size(32, 32));

            float[] input = new float[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Color pixel = resized.GetPixel(x, y);
                    int gray = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    input[y * 32 + x] = gray / 255f;
                }
            }

            return input;
        }

        private static readonly Dictionary<int, TrafficSignInfo> SignsInfo = new Dictionary<int, TrafficSignInfo>()
        {
            {0, new TrafficSignInfo { Name = "Обмеження швидкості 20 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 20 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {1, new TrafficSignInfo { Name = "Обмеження швидкості 30 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 30 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {2, new TrafficSignInfo { Name = "Обмеження швидкості 50 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 50 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {3, new TrafficSignInfo { Name = "Обмеження швидкості 60 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 60 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {4, new TrafficSignInfo { Name = "Обмеження швидкості 70 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 70 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {5, new TrafficSignInfo { Name = "Обмеження швидкості 80 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 80 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {6, new TrafficSignInfo { Name = "Кінець обмеження швидкості 80 км/год", Type = "Інформаційний знак", Purpose = "Скасовує дію знаку обмеження швидкості", Description = "Позначає місце, де закінчується дія знаку обмеження швидкості" }},
            {7, new TrafficSignInfo { Name = "Обмеження швидкості 100 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 100 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {8, new TrafficSignInfo { Name = "Обмеження швидкості 120 км/год", Type = "Заборонний знак", Purpose = "Обмежує максимальну швидкість руху до 120 км/год", Description = "Забороняє рух транспортних засобів зі швидкістю, що перевищує зазначену на знаку" }},
            {9, new TrafficSignInfo { Name = "Обгін заборонено", Type = "Заборонний знак", Purpose = "Забороняє обгін усіх транспортних засобів", Description = "Забороняє обгін усіх транспортних засобів, крім тихохідних, гужових возів, мопедів і двоколісних мотоциклів без бічного причепу" }},
            {10, new TrafficSignInfo { Name = "Обгін заборонено для вантажівок понад 3.5 т", Type = "Заборонний знак", Purpose = "Забороняє обгін для вантажних автомобілів масою понад 3,5 т", Description = "Забороняє вантажним автомобілям з дозволеною максимальною масою понад 3,5 т обгін усіх транспортних засобів" }},
            {11, new TrafficSignInfo { Name = "Головна дорога", Type = "Знак пріоритету", Purpose = "Надає право переважного проїзду", Description = "Позначає дорогу, на якій надається право переважного проїзду нерегульованих перехресть" }},
            {12, new TrafficSignInfo { Name = "Головна дорога", Type = "Знак пріоритету", Purpose = "Надає право переважного проїзду", Description = "Позначає дорогу, на якій надається право переважного проїзду нерегульованих перехресть" }},
            {13, new TrafficSignInfo { Name = "Увага! Дати дорогу", Type = "Знак пріоритету", Purpose = "Вимагає поступитися дорогою транспорту на головній дорозі", Description = "Водій повинен поступитися дорогою транспортним засобам, що рухаються по головній дорозі" }},
            {14, new TrafficSignInfo { Name = "Стоп", Type = "Знак пріоритету", Purpose = "Вимагає обов'язкової зупинки", Description = "Забороняє рух без зупинки перед стоп-лінією, а якщо її немає - перед краєм проїжджої частини" }},
            {15, new TrafficSignInfo { Name = "Рух заборонено", Type = "Заборонний знак", Purpose = "Забороняє рух усіх транспортних засобів", Description = "Забороняє рух усіх транспортних засобів у обох напрямках" }},
            {16, new TrafficSignInfo { Name = "Рух вантажівок понад 3.5 т заборонено", Type = "Заборонний знак", Purpose = "Забороняє рух вантажних автомобілів", Description = "Забороняє рух вантажних автомобілів і складів транспортних засобів з дозволеною максимальною масою понад 3,5 т" }},
            {17, new TrafficSignInfo { Name = "В'їзд заборонено", Type = "Заборонний знак", Purpose = "Забороняє в'їзд усіх транспортних засобів", Description = "Забороняє в'їзд усіх транспортних засобів у дану ділянку дороги" }},
            {18, new TrafficSignInfo { Name = "Загальна небезпека", Type = "Попереджувальний знак", Purpose = "Попереджає про ділянку дороги з підвищеною небезпекою", Description = "Позначає ділянку дороги, рух по якій вимагає заходів, що відповідають обстановці" }},
            {19, new TrafficSignInfo { Name = "Небезпечний поворот ліворуч", Type = "Попереджувальний знак", Purpose = "Попереджає про небезпечний поворот ліворуч", Description = "Позначає крутий поворот або поворот з обмеженою видимістю ліворуч" }},
            {20, new TrafficSignInfo { Name = "Небезпечний поворот праворуч", Type = "Попереджувальний знак", Purpose = "Попереджає про небезпечний поворот праворуч", Description = "Позначає крутий поворот або поворот з обмеженою видимістю праворуч" }},
            {21, new TrafficSignInfo { Name = "Подвійний поворот", Type = "Попереджувальний знак", Purpose = "Попереджає про ділянку дороги з декількома поворотами", Description = "Позначає ділянку дороги з двома або більше слідуючими один за одним поворотами" }},
            {22, new TrafficSignInfo { Name = "Нерівна дорога", Type = "Попереджувальний знак", Purpose = "Попереджає про нерівності на проїжджій частині", Description = "Позначає ділянку дороги, що має нерівності на проїжджій частині" }},
            {23, new TrafficSignInfo { Name = "Слизька дорога", Type = "Попереджувальний знак", Purpose = "Попереджає про ділянку дороги з підвищеною слизькістю", Description = "Позначає ділянку дороги з підвищеним коефіцієнтом ковзання" }},
            {24, new TrafficSignInfo { Name = "Дорога звужується справа", Type = "Попереджувальний знак", Purpose = "Попереджає про звуження дороги справа", Description = "Позначає звуження проїжджої частини попереду справа" }},
            {25, new TrafficSignInfo { Name = "Дорожні роботи", Type = "Попереджувальний знак", Purpose = "Попереджає про проведення дорожніх робіт", Description = "Позначає ділянку дороги, на якій проводяться дорожні роботи" }},
            {26, new TrafficSignInfo { Name = "Світлофорний об'єкт", Type = "Попереджувальний знак", Purpose = "Попереджає про наближення до світлофора", Description = "Позначає наближення до перехрестя, пішохідного переходу або ділянки дороги, рух на якій регулюється світлофором" }},
            {27, new TrafficSignInfo { Name = "Пішоходи", Type = "Попереджувальний знак", Purpose = "Попереджає про місця частого переходу пішоходів", Description = "Позначає ділянку дороги, на якій можлива поява пішоходів" }},
            {28, new TrafficSignInfo { Name = "Діти", Type = "Попереджувальний знак", Purpose = "Попереджає про місця можливої появи дітей", Description = "Позначає ділянку дороги, на якій можлива поява дітей (біля шкіл, дитячих установ)" }},
            {29, new TrafficSignInfo { Name = "Велосипедна доріжка", Type = "Попереджувальний знак", Purpose = "Попереджає про перетинання з велосипедною доріжкою", Description = "Позначає місце перетинання дороги з велосипедною доріжкою" }},
            {30, new TrafficSignInfo { Name = "Обережно, слизько", Type = "Попереджувальний знак", Purpose = "Попереджає про слизьку дорогу", Description = "Позначає ділянку дороги з підвищеною слизькістю покриття" }},
            {31, new TrafficSignInfo { Name = "Перетинання з дикими тваринами", Type = "Попереджувальний знак", Purpose = "Попереджає про можливу появу диких тварин", Description = "Позначає ділянки доріг, що проходять поблизу заповідників або місць частої появи диких тварин" }},
            {32, new TrafficSignInfo { Name = "Кінець усіх обмежень", Type = "Інформаційний знак", Purpose = "Скасовує дію всіх заборонних знаків", Description = "Позначає одночасне закінчення дії всіх заборонних знаків" }},
            {33, new TrafficSignInfo { Name = "Поворот праворуч", Type = "Наказний знак", Purpose = "Дозволяє рух тільки праворуч", Description = "Дозволяє рух тільки в напрямках, зазначених на знаку стрілками" }},
            {34, new TrafficSignInfo { Name = "Поворот ліворуч", Type = "Наказний знак", Purpose = "Дозволяє рух тільки ліворуч", Description = "Дозволяє рух тільки в напрямках, зазначених на знаку стрілками" }},
            {35, new TrafficSignInfo { Name = "Прямо лише", Type = "Наказний знак", Purpose = "Дозволяє рух тільки прямо", Description = "Дозволяє рух тільки прямо" }},
            {36, new TrafficSignInfo { Name = "Прямо або праворуч", Type = "Наказний знак", Purpose = "Дозволяє рух прямо або праворуч", Description = "Дозволяє рух тільки в напрямках, зазначених на знаку стрілками" }},
            {37, new TrafficSignInfo { Name = "Прямо або ліворуч", Type = "Наказний знак", Purpose = "Дозволяє рух прямо або ліворуч", Description = "Дозволяє рух тільки в напрямках, зазначених на знаку стрілками" }},
            {38, new TrafficSignInfo { Name = "Тримайтеся правого краю", Type = "Наказний знак", Purpose = "Вимагає рух з правого боку", Description = "Вимагає об'їжджати перешкоду з правого боку" }},
            {39, new TrafficSignInfo { Name = "Тримайтеся лівого краю", Type = "Наказний знак", Purpose = "Вимагає рух з лівого боку", Description = "Вимагає об'їжджати перешкоду з лівого боку" }},
            {40, new TrafficSignInfo { Name = "Круговий рух", Type = "Наказний знак", Purpose = "Вказує напрямок руху на перехресті", Description = "Вимагає рух в напрямку, зазначеному стрілками" }},
            {41, new TrafficSignInfo { Name = "Кінець заборони обгону", Type = "Інформаційний знак", Purpose = "Скасовує дію знаку заборони обгону", Description = "Позначає закінчення дії знаку «Обгін заборонено»" }},
            {42, new TrafficSignInfo { Name = "Кінець заборони обгону вантажівок понад 3.5 т", Type = "Інформаційний знак", Purpose = "Скасовує дію знаку заборони обгону для вантажівок", Description = "Позначає закінчення дії знаку «Обгін заборонено для вантажних автомобілів»" }}
        };
    }
}

