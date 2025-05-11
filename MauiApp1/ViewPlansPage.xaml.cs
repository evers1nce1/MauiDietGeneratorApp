using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MauiApp1
{
    public partial class ViewPlansPage : ContentPage
    {
        private DatabaseService _databaseService;

        public ViewPlansPage(DatabaseService databaseService)
        {
            InitializeComponent();
            _databaseService = databaseService;
            LoadPlans();
        }

        private async void LoadPlans()
        {
            var plans = await _databaseService.GetSavedPlans();
            PlansCollectionView.ItemsSource = plans;
        }
        private async void OnRemoveClicked(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            if (button?.CommandParameter is SavedMealPlan planToDelete)
            {
                bool confirm = await DisplayAlert("Удаление", $"Удалить план '{planToDelete.Name}'?", "Да", "Нет");
                if (confirm)
                {
                    await _databaseService.DeleteMealPlanItems(planToDelete.Id);
                    await _databaseService.DeleteMealPlan(planToDelete.Id);
                    LoadPlans();
                }
            }
        }
        private async void OnPlanTapped(object sender, EventArgs e)
        {
            if (sender is Frame frame && frame.BindingContext is SavedMealPlan selectedPlan)
            {
                try
                {
                    var planDetails = await _databaseService.LoadMealPlan(selectedPlan.Id);
                    if (planDetails == null)
                    {
                        await DisplayAlert("Ошибка", "Детали плана не найдены.", "Ок");
                        return;
                    }

                    await Navigation.PushAsync(new MealPlanDetailsPage(planDetails));
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", ex.Message, "Ок");
                }
            }
        }
    }
}
