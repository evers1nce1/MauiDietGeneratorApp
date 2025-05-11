using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MauiApp1;

public partial class ProductsPage : ContentPage
{
    DatabaseService _databaseService;
    public ObservableCollection<Product> Products { get; set; } = new();
    private bool _isMenuOpen = false;
    public ProductsPage()
    {
        _databaseService = new DatabaseService();
        InitializeComponent();
        LoadProducts();
        ProductsCollectionView.ItemsSource = Products;
    }

    private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Product selectedProduct)
        {
            await DisplayAlert(
                selectedProduct.Name,
                $"Калорийность: {selectedProduct.Calories} ккал",
                "OK");

            ProductsCollectionView.SelectedItem = null;
        }
    }
    private void OnProductTapped(object sender, EventArgs e)
    {
        if (sender is Label label && label.BindingContext is Product tappedProduct)
        {
            tappedProduct.IsExpanded = !tappedProduct.IsExpanded;
        }
    }

    private async void OnAppSettingsClicked(object sender, EventArgs e)
    {
        await LeftMenu.TranslateTo(-200, 0, 250, Easing.CubicIn);
        _isMenuOpen = false;
        await Navigation.PushAsync(new SettingsPage(_databaseService));
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Product productToEdit)
        {
            string newName = await DisplayPromptAsync("Редактирование", "Новое название продукта:", initialValue: productToEdit.Name);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                productToEdit.Name = newName;
                ProductsCollectionView.ItemsSource = null;
                ProductsCollectionView.ItemsSource = Products;
            }
        }

    }
    private async void OnRemoveClicked(object sender, EventArgs e)
    {

        if (sender is ImageButton button)
        {

            if (button.BindingContext is Product productToRemove)
            {

                bool confirm = await DisplayAlert(
                    "Подтверждение",
                    $"Удалить {productToRemove.Name}?",
                    "Да", "Нет");

                if (confirm)
                {
                    int result = await _databaseService.DeleteProductAsync(productToRemove.Id);
                    if (result > 0)
                    {
                        Products.Remove(productToRemove);
                    }
                }
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось определить продукт", "OK");
            }
        }
    }
    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        var popupPage = new AddProductPopupPage();
        await Navigation.PushModalAsync(popupPage);

        popupPage.Disappearing += async (s, args) =>
        {
            if (popupPage.ResultProduct != null)
            {
                await _databaseService.AddProductAsync(popupPage.ResultProduct);

                Products.Add(popupPage.ResultProduct);

                await DisplayAlert("Успех", $"{popupPage.ResultProduct.Name} добавлен!", "OK");
            }
        };
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        if (_isMenuOpen)
            await LeftMenu.TranslateTo(-200, 0, 250, Easing.CubicIn);
        else
            await LeftMenu.TranslateTo(0, 0, 250, Easing.CubicOut);

        _isMenuOpen = !_isMenuOpen;
    }

    private async void OnProductsClicked(object sender, EventArgs e)
    {
        await LeftMenu.TranslateTo(-200, 0, 250, Easing.CubicIn);
        _isMenuOpen = false;
    }

    private async void OnRecipesClicked(object sender, EventArgs e)
    {
        await LeftMenu.TranslateTo(-200, 0, 250, Easing.CubicIn);
        _isMenuOpen = false;
        await Navigation.PushAsync(new ViewPlansPage(_databaseService));
    }

    private async void OnRecipesGeneratorClicked(object sender, EventArgs e)
    {
        await LeftMenu.TranslateTo(-200, 0, 250, Easing.CubicIn);
        _isMenuOpen = false;
        await Navigation.PushAsync(new RecipeGeneratorPage(_databaseService, Products));
    }


    private async void LoadProducts()
    {
        var products = await _databaseService.GetAllProductsAsync();
        Products.Clear();
        foreach (var product in products)
        {
            Products.Add(product); 
        }
    }

    private void OnSearchPressed(object sender, EventArgs e)
    {
        var searchText = ProductSearchBar.Text?.ToLower() ?? "";
        ProductsCollectionView.ItemsSource = Products.Where(p => p.Name.ToLower().Contains(searchText)).ToList();
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Фильтры", "Здесь будут фильтры для продуктов.", "OK");
    }
}
