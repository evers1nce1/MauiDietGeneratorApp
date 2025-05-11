using Microsoft.Maui.Controls;

namespace MauiApp1
{
    public partial class DietGenerationPage : ContentPage
    {
        private readonly DatabaseService _dbService;
        private readonly double _targetCalories;
        private readonly DietOptimizer.DietGoal _dietGoal;
        private readonly string _planName;
        private DietOptimizer.MealPlan _generatedPlan;

        public DietGenerationPage(
            DatabaseService dbService,
            double targetCalories,
            DietOptimizer.DietGoal dietGoal,
            string planName)
        {
            InitializeComponent();

            _dbService = dbService;
            _targetCalories = targetCalories;
            _dietGoal = dietGoal;
            _planName = planName;

                        GoalLabel.Text = _dietGoal switch
            {
                DietOptimizer.DietGoal.Gain => "Цель: Набор веса",
                DietOptimizer.DietGoal.Lose => "Цель: Сброс веса",
                _ => "Цель: Поддержание веса"
            };

            CaloriesLabel.Text = $"Целевые калории: {_targetCalories:F0} ккал";
            Title = _planName;

                        _ = GeneratePlanAsync();
        }

        private async Task GeneratePlanAsync()
        {
            try
            {
                _generatedPlan = await _dbService.GenerateDietPlan(_targetCalories, _dietGoal);
                DisplayGeneratedPlan(_generatedPlan);
                ResultLayout.IsVisible = true;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
            }
        }

        private void DisplayGeneratedPlan(DietOptimizer.MealPlan plan)
        {
            MealsStack.Children.Clear();

            AddMealSection("Завтрак", plan.Breakfast);
            AddMealSection("Обед", plan.Lunch);
            AddMealSection("Ужин", plan.Dinner);

            MealsStack.Children.Add(new Label
            {
                Text = $"Итого: {plan.TotalCalories:F0} ккал | " +
                      $"Б: {plan.TotalProteins:F1}г | " +
                      $"Ж: {plan.TotalFats:F1}г | " +
                      $"У: {plan.TotalCarbs:F1}г",
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black             });
        }

        private void AddMealSection(string title, List<Product> products)
        {
            var stack = new StackLayout { Spacing = 8 };

                        stack.Children.Add(new Label
            {
                Text = title,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 5),
                TextColor = Colors.Black             });

            foreach (var product in products)
            {
                                var mainRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10,
                    Margin = new Thickness(15, 0, 0, 0)
                };

                mainRow.Children.Add(new Label
                {
                    Text = $"• {product.Name}",
                    FontSize = 14,
                    WidthRequest = 150,                     TextColor = Colors.Black                 });

                mainRow.Children.Add(new Label
                {
                    Text = $"{product.Weight:F0}г",
                    FontSize = 14,
                    TextColor = Colors.Black                 });

                mainRow.Children.Add(new Label
                {
                    Text = $"{product.DisplayCalories:F0} ккал",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black                 });

                stack.Children.Add(mainRow);

                                var nutrientsRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 15,
                    Margin = new Thickness(30, 0, 0, 5)
                };

                nutrientsRow.Children.Add(new Label
                {
                    Text = $"Б: {product.DisplayProteins:F1}г",
                    FontSize = 12,
                    TextColor = Colors.Black                 });

                nutrientsRow.Children.Add(new Label
                {
                    Text = $"Ж: {product.DisplayFats:F1}г",
                    FontSize = 12,
                    TextColor = Colors.Black                 });

                nutrientsRow.Children.Add(new Label
                {
                    Text = $"У: {product.DisplayCarbs:F1}г",
                    FontSize = 12,
                    TextColor = Colors.Black                 });

                stack.Children.Add(nutrientsRow);
            }

            MealsStack.Children.Add(stack);
        }
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_generatedPlan == null) return;

            try
            {
                await _dbService.SaveMealPlan(_generatedPlan, _planName);
                await DisplayAlert("Сохранено", $"План '{_planName}' сохранен", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }
    }
}