namespace MauiApp1; 
public partial class AddProductPopupPage : ContentPage
{
    public Product ResultProduct { get; private set; }

    public AddProductPopupPage()
    {
        InitializeComponent();
    }
    private async void OnAutoFillClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Ошибка", "Введите название продукта", "OK");
            return;
        }

        try
        {
            var llm = new LLMService();
            var info = await llm.GetProductInfoAsync(name);

            if (info == null)
            {
                await DisplayAlert("Ошибка", "Не удалось получить данные от модели", "OK");
                return;
            }
            CategoryPicker.SelectedItem = info.Category;
            CaloriesEntry.Text = info.Calories.ToString("0.0");
            ProteinsEntry.Text = info.Proteins.ToString("0.0");
            FatsEntry.Text = info.Fats.ToString("0.0");
            CarbsEntry.Text = info.Carbs.ToString("0.0");
            WeightEntry.Text = "100";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка запроса: {ex.Message}", "OK");
        }
    }

    private void OnAddClicked(object sender, EventArgs e)
    {
        if (CategoryPicker.SelectedIndex == -1)
        {
            DisplayAlert("Ошибка", "Выберите категорию", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            DisplayAlert("Ошибка", "Введите название продукта", "OK");
            return;
        }

        if (!double.TryParse(CaloriesEntry.Text, out double calories) || calories <= 0)
        {
            DisplayAlert("Ошибка", "Некорректная калорийность", "OK");
            return;
        }

        if (!double.TryParse(WeightEntry.Text, out double weight) || calories <= 0)
        {
            DisplayAlert("Ошибка", "Некорректная калорийность", "OK");
            return;
        }
        ProductCategory category = CategoryPicker.SelectedItem switch
        {
            "Мясо" => ProductCategory.Meat,
            "Молочные продукты" => ProductCategory.Dairy,
            "Зерновые" => ProductCategory.Grain,
            "Овощи" => ProductCategory.Vegetable,
            "Фрукты" => ProductCategory.Fruit,
            "Бобовые" => ProductCategory.Legume,
            "Яйца" => ProductCategory.Egg,
            "Рыба" => ProductCategory.Fish,
            "Масла" => ProductCategory.Oil,
            "Сладости" => ProductCategory.Sweet,
            _ => ProductCategory.Unknown,
        };


        ResultProduct = new Product
        {
            Name = NameEntry.Text,
            Calories = calories,
            Category = category,
            Proteins = double.TryParse(ProteinsEntry.Text, out double p) ? p : 0,
            Fats = double.TryParse(FatsEntry.Text, out double f) ? f : 0,
            Carbs = double.TryParse(CarbsEntry.Text, out double c) ? c : 0,
            Weight = weight
        };

        Navigation.PopModalAsync();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        ResultProduct = null;
        Navigation.PopModalAsync();
    }
}