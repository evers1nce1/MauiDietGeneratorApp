using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MauiApp1
{
    public class LLMService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;
        private readonly string _model = "qwen2.5-3b-instruct-q5_0";

        public LLMService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Preferences.Get("api_url", "http://localhost:1337"))
            };
            _apiUrl = "/v1/chat/completions"; 
        }

        public async Task<RecipeResponse> GetRecipeAsync(string[] ingredients)
        {
            var prompt = $@"
Ты - AI шеф-повар с диетологическим образованием. Сгенерируй рецепт на основе предоставленных ингредиентов. Ответ предоставь строго в формате JSON:


{{
""name"": ""Название блюда (на языке пользователя)"",
""category"": ""Основная категория (Meat/Dairy/Grain/Vegetable/Fruit/Legume/Egg/Fish/Oil/Sweet)"",
""calories"": число, // Общая калорийность на порцию
""proteins"": число, // Белки в граммах на порцию
""fats"": число, // Жиры в граммах на порцию
""carbs"": число, // Углеводы в граммах на порцию
""recipe"": [ // Пошаговая инструкция
""Шаг 1..."",
""Шаг 2..."",
""Шаг 3...""
]
}}


Требования:


Название: отражает состав, состоит из 1-4 слов
Категория: должна быть ОДНА, выбирается по преобладающему ингредиенту
Нутриенты: точный расчет суммы всех ингредиентов
Рецепт: 3-5 практичных шагов с указанием:
Техники приготовления (жарка, запекание и т.д.)
Времени каждого этапа
Температурного режима (где применимо)
Порция: 100 грамм
Учитывай:
Кулинарную сочетаемость
Баланс вкусов
Технику безопасности


Пример запроса: ""куриная грудка, брокколи, рис""
Пример ответа:
{{
""name"": ""Запеченная курица с брокколи и рисом"",
""category"": ""Meat"",
""calories"": 150,
""proteins"": 15,
""fats"": 6,
""carbs"": 12,
""recipe"": [
""Разогрей духовку до 200°C. Куриную грудку натри специями."",
""Выложи курицу на противень, вокруг разложи соцветия брокколи."",
""Запекай 25 минут до золотистой корочки."",
""Подавай с отварным рисом.""
]
}}
Ингредиенты: {string.Join(',', ingredients)}
";

            var request = new
            {

                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
                return null;

            var resultJson = await response.Content.ReadAsStringAsync();
            return ParseRecipeResponse(resultJson);
        }

        private RecipeResponse ParseRecipeResponse(string jsonResponse)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.GetArrayLength() == 0 ||
                    !choices[0].TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("content", out var content))
                {
                    Debug.WriteLine("Неверная структура ответа API");
                    return null;
                }

                string recipeJson = content.GetString();

                recipeJson = recipeJson.Replace("```json", "")
                                      .Replace("```", "")
                                      .Trim();
                Debug.Print(recipeJson);
                return JsonSerializer.Deserialize<RecipeResponse>(recipeJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка парсинга: {ex.Message}");
                return null;
            }
        }
        public async Task<ProductLLMInfo> GetProductInfoAsync(string productName)
        {
            var prompt = $@"
    Определи категорию продукта и его пищевую ценность (на 100г продукта). Категория должна быть одной из следующих:
Мясо, Молочные продукты, Зерновые, Овощи, Фрукты, Бобовые, Яйца, Рыба, Масла, Сладости.

    Если продукт неизвестен, то вернуть
    {{
        ""Category"": ""Без категории"",
        ""Calories"": 0,
        ""Proteins"": 0,
        ""Fats"": 0,
        ""Carbs"": 0
    }}

    Для продукта: ""{productName}""

    Ответь строго в формате JSON:
    {{
        ""Category"": ""..."",
        ""Calories"": ...,
        ""Proteins"": ...,
        ""Fats"": ...,
        ""Carbs"": ...
    }}";

            var request = new
            {
                model = _model,
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
                return null;

            var resultJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine(resultJson);

            var startIndex = resultJson.IndexOf("\"content\":\"") + "\"content\":\"".Length;
            var endIndex = resultJson.LastIndexOf("\"}") + 1;
            var cleanJson = resultJson.Substring(startIndex, endIndex - startIndex);

            cleanJson = cleanJson.Replace("\\n", "").Replace("\\\"", "\"");

            var jsonStartIndex = cleanJson.IndexOf("{");
            var jsonEndIndex = cleanJson.LastIndexOf("}") + 1;

            if (jsonStartIndex >= 0 && jsonEndIndex > jsonStartIndex)
            {
                var finalJson = cleanJson.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex);

                Debug.WriteLine("Финальный JSON для парсинга: " + finalJson);

                try
                {
                    var parsed = JsonSerializer.Deserialize<ProductLLMInfo>(finalJson);
                    Debug.WriteLine("Десериализованный объект: " + JsonSerializer.Serialize(parsed));
                    return parsed;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Ошибка парсинга JSON: " + ex.Message);
                }
            }

            return null;
        }
    }
    public class RecipeResponse
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public double Calories { get; set; }
        public double Proteins { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
        public List<string> Recipe { get; set; }
    }
    public class ProductLLMInfo
    {
        public string Category { get; set; }
        public double Calories { get; set; }
        public double Proteins { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
    }

}
