using Microsoft.Maui.Controls;
using System.Collections.Generic;

namespace MauiApp1
{
    public partial class MealPlanDetailsPage : ContentPage
    {
        public MealPlanDetailsPage(DietOptimizer.MealPlan plan)
        {
            InitializeComponent();
            DisplayGeneratedPlan(plan);
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
                FontSize = 16,
                TextColor = Colors.Black,                  
                Margin = new Thickness(0, 10, 0, 0)
            });
        }

        private void AddMealSection(string title, List<Product> products)
        {
            var stack = new StackLayout { Spacing = 8 };

            stack.Children.Add(new Label
            {
                Text = title,
                FontAttributes = FontAttributes.Bold,
                FontSize = 18,
                TextColor = Colors.Black,  
                Margin = new Thickness(0, 10, 0, 5)
            });

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
                    TextColor = Colors.Black,
                    WidthRequest = 150
                });

                mainRow.Children.Add(new Label
                {
                    Text = $"{product.Weight:F0}г",
                    FontSize = 14,
                    TextColor = Colors.Black 
                });

                mainRow.Children.Add(new Label
                {
                    Text = $"{product.DisplayCalories:F0} ккал",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.Black 
                });

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
                    TextColor = Colors.Black  
                });

                nutrientsRow.Children.Add(new Label
                {
                    Text = $"Ж: {product.DisplayFats:F1}г",
                    FontSize = 12,
                    TextColor = Colors.Black  
                });

                nutrientsRow.Children.Add(new Label
                {
                    Text = $"У: {product.DisplayCarbs:F1}г",
                    FontSize = 12,
                    TextColor = Colors.Black  
                });

                stack.Children.Add(nutrientsRow);
            }

            MealsStack.Children.Add(stack);
        }
    }
}
