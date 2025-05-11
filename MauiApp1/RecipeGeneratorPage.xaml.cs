using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MauiApp1
{
    public partial class RecipeGeneratorPage : ContentPage, INotifyPropertyChanged
    {
        private string _recipeName;
        private double _calories;
        private double _proteins;
        private RecipeResponse _recipe;
        private double _fats;
        ObservableCollection<Product> Products;
        private double _carbs;
        private ObservableCollection<string> _recipeSteps = new();
        DatabaseService _dbService;
        public string RecipeName
        {
            get => _recipeName;
            set { _recipeName = value; OnPropertyChanged(); }
        }

        public double Calories
        {
            get => _calories;
            set { _calories = value; OnPropertyChanged(); }
        }

        public double Proteins
        {
            get => _proteins;
            set { _proteins = value; OnPropertyChanged(); }
        }

        public double Fats
        {
            get => _fats;
            set { _fats = value; OnPropertyChanged(); }
        }

        public double Carbs
        {
            get => _carbs;
            set { _carbs = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> RecipeSteps
        {
            get => _recipeSteps;
            set { _recipeSteps = value; OnPropertyChanged(); }
        }

        public RecipeGeneratorPage(DatabaseService _dbService, ObservableCollection<Product> products)
        {

            InitializeComponent();
            BindingContext = this;
            _recipe = null;
            Products = products;
            this._dbService = _dbService;
            GenerateButton.Clicked += async (s, e) => await GenerateRecipe();
        }

        private async Task GenerateRecipe()
        {
            try
            {
                ErrorLabel.IsVisible = false;
                LoadingIndicator.IsVisible = true;

                var ingredients = IngredientsEntry.Text.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (ingredients.Length == 0)
                {
                    ErrorLabel.Text = "Введите хотя бы один ингредиент";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                var llmService = new LLMService();
                _recipe = await llmService.GetRecipeAsync(ingredients);

                if (_recipe != null)
                {
                    RecipeNameLabel.Text = _recipe.Name;
                    RecipeName = _recipe.Name;
                    Calories = _recipe.Calories;
                    Proteins = _recipe.Proteins;
                    Fats = _recipe.Fats;
                    Carbs = _recipe.Carbs;
                    RecipeSteps = new ObservableCollection<string>(_recipe.Recipe);
                    ResultFrame.IsVisible = true;
                    AddRecipeButton.IsVisible = true;

                }
                else
                {
                    ErrorLabel.Text = "Не удалось сгенерировать рецепт";
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Ошибка: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
            }
        }
        private async void OnAddRecipeClicked(object sender, EventArgs e)
        {

            ProductCategory category = _recipe.Category switch
            {
                "Meat" => ProductCategory.Meat,
                "Dairy" => ProductCategory.Dairy,
                "Grain" => ProductCategory.Grain,
                "Vegetable" => ProductCategory.Vegetable,
                "Fruit" => ProductCategory.Fruit,
                "Legume" => ProductCategory.Legume,
                "Egg" => ProductCategory.Egg,
                "Fish" => ProductCategory.Fish,
                "Oil" => ProductCategory.Oil,
                "Sweet" => ProductCategory.Sweet,
                _ => ProductCategory.Unknown,
            };

            Product recipeToAdd = new Product
            {
                Name = _recipe.Name,
                Calories = _recipe.Calories,
                Proteins = _recipe.Proteins,
                Fats = _recipe.Fats,
                Carbs = _recipe.Carbs,
                Weight = 100,
                Category = category
            };
            Products.Add(recipeToAdd);
            await _dbService.AddProductAsync(recipeToAdd);
            IngredientsEntry.Text = string.Empty;
            ResultFrame.IsVisible = false;
            AddRecipeButton.IsVisible = false;
            await DisplayAlert("Успешно", "Рецепт был добавлен в вашу коллекцию", "OK");

        }

    }
}