namespace MauiApp1
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _dbService;

        private readonly List<string> _activityLevels = new()
        {
            "Сидячий образ жизни (мало движений)",
            "Легкая активность (1-3 тренировки в неделю)",
            "Умеренная активность (3-5 тренировок)",
            "Высокая активность (6-7 тренировок)",
            "Экстремальная активность (тяжелая работа+тренировки)"
        };
        private readonly double[] _activityMultipliers = { 1.2, 1.375, 1.55, 1.725, 1.9 };

        public SettingsPage(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            ActivityPicker.ItemsSource = _activityLevels;
            LoadUserData();
        }
        private void LoadUserData()
        {
            ApiUrlEntry.Text = Preferences.Get("api_url", "http://localhost:1337").ToString();
            WeightEntry.Text = Preferences.Get("user_weight", 80.0).ToString();
            HeightEntry.Text = Preferences.Get("user_height", 180).ToString();
            AgeEntry.Text = Preferences.Get("user_age", 18).ToString();
            bool isMale = Preferences.Get("user_is_male", true);
            RadioMale.IsChecked = isMale;
            RadioFemale.IsChecked = !isMale;
            string goal = Preferences.Get("user_goal", "Поддержание веса");
            GainWeightRadio.IsChecked = goal == "Набор веса";
            MaintainWeightRadio.IsChecked = goal == "Поддержание веса";
            LoseWeightRadio.IsChecked = goal == "Сброс веса";
            ActivityPicker.SelectedIndex = Preferences.Get("user_activity_level", 0);
        }
        private async void OnGenerateDietClicked(object sender, EventArgs e)
        {
            if (!Preferences.ContainsKey("user_tdee"))
            {
                await DisplayAlert("Внимание", "Пожалуйста, сначала сохраните настройки", "OK");
                return;
            }

            double tdee = Preferences.Get("user_tdee", 2000.0);
            string goal = Preferences.Get("user_goal", "Поддержание веса");

            var dietGoal = goal switch
            {
                "Набор веса" => DietOptimizer.DietGoal.Gain,
                "Сброс веса" => DietOptimizer.DietGoal.Lose,
                _ => DietOptimizer.DietGoal.Maintain
            };

            double targetCalories = goal switch
            {
                "Набор веса" => tdee + 300,
                "Сброс веса" => tdee - 300,
                _ => tdee
            };

            var planName = await DisplayPromptAsync(
                "Новый план питания",
                $"Будет создан план для {goal}\nЦелевые калории: {targetCalories:F0}",
                placeholder: "Мой план питания");

            if (!string.IsNullOrWhiteSpace(planName))
            {
                var generationPage = new DietGenerationPage(_dbService, targetCalories, dietGoal, planName);
                await Navigation.PushAsync(generationPage);
            }
        }
        private void OnEntryValidationNeeded(object sender, FocusEventArgs e)
        {
            ValidateEntry(sender as Entry);
        }

        private void ValidateEntry(Entry entry)
        {
            if (entry == null) return;

            var (min, max) = entry switch
            {
                _ when entry == HeightEntry => (100, 250),
                _ when entry == WeightEntry => (40, 200),
                _ => (18, 99)
            };

            if (string.IsNullOrEmpty(entry.Text))
            {
                entry.Text = min.ToString();
                return;
            }

            if (!int.TryParse(entry.Text, out int value))
            {
                entry.Text = min.ToString();
                return;
            }

            entry.Text = Math.Clamp(value, min, max).ToString();
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            double weight = string.IsNullOrEmpty(WeightEntry.Text) ? 0 : double.Parse(WeightEntry.Text);
            int height = string.IsNullOrEmpty(HeightEntry.Text) ? 0 : int.Parse(HeightEntry.Text);
            int age = string.IsNullOrEmpty(AgeEntry.Text) ? 0 : int.Parse(AgeEntry.Text);
            int activityIndex = ActivityPicker.SelectedIndex;
            double multiplier = _activityMultipliers[activityIndex];

            string goal = "";
            bool isMale = false;
            if (RadioMale.IsChecked)
                isMale = true;
            else
                isMale = false;
            if (GainWeightRadio.IsChecked) goal = "Набор веса";
            else if (MaintainWeightRadio.IsChecked) goal = "Поддержание веса";
            else if (LoseWeightRadio.IsChecked) goal = "Сброс веса";
            double baseMetabolism = Utils.CalcBaseMetabolism(weight, height, age, isMale);
            double fixedMetabolism = Math.Round(baseMetabolism * multiplier, 1);
            string url = ApiUrlEntry.Text.Trim();
            string url_api = url == null ? string.Empty : url;
            Preferences.Set("api_url", url_api);
            Preferences.Set("user_weight", weight);
            Preferences.Set("user_height", height);
            Preferences.Set("user_age", age);
            Preferences.Set("user_is_male", isMale);
            Preferences.Set("user_goal", goal);
            Preferences.Set("user_activity_level", activityIndex);
            Preferences.Set("user_bmr", baseMetabolism);
            Preferences.Set("user_tdee", fixedMetabolism);

            DisplayAlert("Сохранено",
                $"Данные сохранены:\nВес: {weight} кг\nРост: {height} см\nВозраст: {age}\nЦель: {goal}\nБазовый метаболизм: {baseMetabolism} ккал\nРасход калорий: {fixedMetabolism} ккал",
                "OK");
        }
    }
}