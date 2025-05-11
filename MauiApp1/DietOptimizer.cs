using System;
using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;

namespace MauiApp1
{
    public class DietOptimizer
    {
        public enum DietGoal { Maintain, Gain, Lose }
        public class MealPlan
        {
            public List<Product> Breakfast { get; set; } = new List<Product>();
            public List<Product> Lunch { get; set; } = new List<Product>();
            public List<Product> Dinner { get; set; } = new List<Product>();
            public HashSet<int> UsedProductIds { get; } = new HashSet<int>();

            public void CalculateTotals()
            {
                TotalCalories = Breakfast.Sum(p => p.Calories) + Lunch.Sum(p => p.Calories) + Dinner.Sum(p => p.Calories);
                TotalProteins = Breakfast.Sum(p => p.Proteins) + Lunch.Sum(p => p.Proteins) + Dinner.Sum(p => p.Proteins);
                TotalFats = Breakfast.Sum(p => p.Fats) + Lunch.Sum(p => p.Fats) + Dinner.Sum(p => p.Fats);
                TotalCarbs = Breakfast.Sum(p => p.Carbs) + Lunch.Sum(p => p.Carbs) + Dinner.Sum(p => p.Carbs);
            }

            public double TotalCalories { get; set; }
            public double TotalProteins { get; set; }
            public double TotalFats { get; set; }
            public double TotalCarbs { get; set; }

            public override string ToString()
            {
                return $"Calories: {TotalCalories:F1}, Proteins: {TotalProteins:F1}g, Fats: {TotalFats:F1}g, Carbs: {TotalCarbs:F1}g";
            }
        }

        public MealPlan GenerateDietPlan(List<Product> allProducts, double dailyCalories, DietGoal goal)
        {
            if (allProducts == null || allProducts.Count < 10)
                throw new ArgumentException("Добавьте хотя бы 10 продуктов!");


            var mealRatios = GetMealRatios(goal);
            var targets = CalculateNutritionTargets(dailyCalories, goal);
            var filteredProducts = FilterProductsByGoal(allProducts, goal);

            var plan = new MealPlan();

                        plan.Breakfast = OptimizeMealWithSimplex(filteredProducts, dailyCalories * mealRatios.BreakfastRatio,
                targets, goal, isBreakfast: true, plan.UsedProductIds);

                        var remainingProducts = filteredProducts
                .Where(p => !plan.UsedProductIds.Contains(p.Id))
                .ToList();

            plan.Lunch = OptimizeMealWithSimplex(remainingProducts, dailyCalories * mealRatios.LunchRatio,
                targets, goal, isBreakfast: false, plan.UsedProductIds);

                        remainingProducts = remainingProducts
                .Where(p => !plan.UsedProductIds.Contains(p.Id))
                .ToList();

            plan.Dinner = OptimizeMealWithSimplex(remainingProducts, dailyCalories * mealRatios.DinnerRatio,
                targets, goal, isBreakfast: false, plan.UsedProductIds);

            plan.CalculateTotals();
            return plan;
        }
        private double GetNutrientScore(Product p, DietGoal goal) =>
            goal switch
            {
                DietGoal.Gain => p.Proteins * 0.8 + p.Calories * 0.3 - p.Fats * 0.4,
                DietGoal.Lose => p.Proteins * 0.7 + p.Carbs * 0.1 - p.Fats * 0.2,
                _ => p.Proteins * 0.5 + p.Carbs * 0.3 + p.Fats * 0.2
            };

        private List<Product> OptimizeMealWithSimplex(List<Product> products, double targetCalories,
            (double Proteins, double Fats, double Carbs) targets, DietGoal goal,
            bool isBreakfast, HashSet<int> usedProducts)
        {
                        double proteinTarget = isBreakfast ? targets.Proteins * 0.3 : targets.Proteins * 0.35;
            double fatTarget = isBreakfast ? targets.Fats * 0.3 : targets.Fats * 0.35;
            double carbTarget = isBreakfast ? targets.Carbs * 0.3 : targets.Carbs * 0.35;

                        double minCalories = targetCalories * 0.9;

            double maxCalories = targetCalories * 1.1;
                        var availableProducts = products
                .Where(p => usedProducts == null || !usedProducts.Contains(p.Id))
                .ToList();

            if (availableProducts.Count == 0)
                availableProducts = products.Take(10).ToList(); 
            try
            {
                                var categoryProducts = availableProducts
                    .GroupBy(p => p.Category)
                    .Select(g => g.OrderByDescending(p => GetNutrientScore(p, goal))
                    .First())
                    .ToList();
                                Solver solver = Solver.CreateSolver("GLOP");
                if (solver == null)
                    throw new Exception("Failed to create solver");

                                Variable[] variables = new Variable[categoryProducts.Count];
                for (int i = 0; i < categoryProducts.Count; i++)
                {
                    variables[i] = solver.MakeNumVar(0.5, 2.5, $"p_{categoryProducts[i].Id}");
                }

                
                                Constraint calorieConstraint = solver.MakeConstraint(minCalories, maxCalories, "calories");
                for (int i = 0; i < categoryProducts.Count; i++)
                {
                    calorieConstraint.SetCoefficient(variables[i], categoryProducts[i].Calories);
                }

                                Constraint proteinConstraint = solver.MakeConstraint(proteinTarget * 0.8, proteinTarget * 1.2, "protein");
                Constraint fatConstraint = solver.MakeConstraint(fatTarget * 0.8, fatTarget * 1.2, "fat");
                Constraint carbConstraint = solver.MakeConstraint(carbTarget * 0.8, carbTarget * 1.2, "carbs");

                for (int i = 0; i < categoryProducts.Count; i++)
                {
                    proteinConstraint.SetCoefficient(variables[i], categoryProducts[i].Proteins);
                    fatConstraint.SetCoefficient(variables[i], categoryProducts[i].Fats);
                    carbConstraint.SetCoefficient(variables[i], categoryProducts[i].Carbs);
                }

                                Objective objective = solver.Objective();
                for (int i = 0; i < categoryProducts.Count; i++)
                {
                    var p = categoryProducts[i];
                                        double score = GetNutrientScore(p, goal);
                    objective.SetCoefficient(variables[i], score);
                }
                objective.SetMaximization();

                                var resultStatus = solver.Solve();

                                if (resultStatus == Solver.ResultStatus.OPTIMAL)
                {
                    var selectedProducts = new List<Product>();
                    double totalCalories = 0;

                    for (int i = 0; i < categoryProducts.Count; i++)
                    {
                        double portion = variables[i].SolutionValue();
                        if (portion >= 0.5)                         {
                            var product = categoryProducts[i];
                            var scaledProduct = ScaleProduct(product, portion);
                            selectedProducts.Add(scaledProduct);
                            totalCalories += scaledProduct.Calories;
                            usedProducts?.Add(product.Id);
                        }
                    }

                                        if (totalCalories >= minCalories)
                    {
                        return selectedProducts;
                    }
                }

                                return GetFallbackMealWithConstraints(availableProducts, targetCalories, usedProducts);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Optimization error: {ex.Message}");
                return GetFallbackMealWithConstraints(availableProducts, targetCalories, usedProducts);
            }
        }

        private List<Product> GetFallbackMealWithConstraints(List<Product> products, double targetCalories, HashSet<int> usedProducts)
        {
                        var candidates = products
                .Where(p => usedProducts == null || !usedProducts.Contains(p.Id))
                .GroupBy(p => p.Category)
                .Select(g => g.OrderByDescending(p => p.Proteins + p.Carbs).First())
                .Take(3)                 .ToList();

            if (candidates.Count == 0)
            {
                candidates = products
                    .Where(p => usedProducts == null || !usedProducts.Contains(p.Id))
                    .Take(3)
                    .ToList();
            }

                        double totalCalories = candidates.Sum(p => p.Calories);
            double scale = Math.Min(5.0, Math.Max(0.5, targetCalories / totalCalories));

            var result = new List<Product>();
            foreach (var product in candidates)
            {
                                double portion = Math.Min(3.0, Math.Max(0.5, scale * (product.Calories / totalCalories * 3)));
                result.Add(ScaleProduct(product, portion));
                usedProducts?.Add(product.Id);
            }

            return result;
        }

        private Product ScaleProduct(Product product, double portion)
        {
                        double scaledWeight = product.Weight * portion;             double ratio = scaledWeight / 100; 
            return new Product
            {
                Id = product.Id,
                Name = $"{product.Name}",
                Calories = Math.Round(product.Calories * ratio, 1),
                Proteins = Math.Round(product.Proteins * ratio, 1),
                Fats = Math.Round(product.Fats * ratio, 1),
                Carbs = Math.Round(product.Carbs * ratio, 1),
                Weight = Math.Round(scaledWeight, 1),
                Category = product.Category,
                IsExpanded = false
            };
        }

        private List<Product> GetFallbackMeal(List<Product> products, double targetCalories, HashSet<int> usedProducts)
        {
            var candidates = products
                .Where(p => usedProducts == null || !usedProducts.Contains(p.Id))
                .OrderByDescending(p => p.Proteins)
                .ThenBy(p => p.Fats)
                .Take(3)
                .ToList();

            if (candidates.Count == 0)
                candidates = products.Take(3).ToList();

            double totalCalories = candidates.Sum(p => p.Calories);
            double scale = targetCalories / totalCalories;

            var result = candidates.Select(p => ScaleProduct(p, scale)).ToList();

            foreach (var product in result)
            {
                usedProducts?.Add(product.Id);
            }

            return result;
        }

        private (double BreakfastRatio, double LunchRatio, double DinnerRatio) GetMealRatios(DietGoal goal)
        {
            return goal switch
            {
                DietGoal.Gain => (0.3, 0.4, 0.3),
                DietGoal.Lose => (0.35, 0.3, 0.35),
                _ => (0.3, 0.35, 0.35)
            };
        }

        private (double Proteins, double Fats, double Carbs) CalculateNutritionTargets(double dailyCalories, DietGoal goal)
        {
            return goal switch
            {
                                DietGoal.Gain => (
                    Proteins: dailyCalories * 0.30 / 4,                      Fats: dailyCalories * 0.25 / 9,                         Carbs: dailyCalories * 0.45 / 4                     ),

                                DietGoal.Lose => (
                    Proteins: dailyCalories * 0.35 / 4,                      Fats: dailyCalories * 0.30 / 9,                          Carbs: dailyCalories * 0.35 / 4                      ),

                                _ => (
                    Proteins: dailyCalories * 0.30 / 4,                      Fats: dailyCalories * 0.30 / 9,                          Carbs: dailyCalories * 0.40 / 4                      )
            };
        }



        private List<Product> FilterProductsByGoal(List<Product> products, DietGoal goal)
        {
                        var baseFiltered = products
                .Where(p => p.Proteins >= 0 && p.Fats >= 0 && p.Carbs >= 0)
                .ToList();

                        baseFiltered = goal switch
            {
                DietGoal.Gain => baseFiltered
                    .Where(p => p.Proteins > 0)                     .OrderBy(p => p.Fats / (p.Proteins + 1.0))                     .ThenByDescending(p => p.Proteins * 0.7 + p.Carbs * 0.3)                     .Where(p => p.Fats <= 20)                     .ToList(),

                DietGoal.Lose => baseFiltered
                    .Where(p => p.Calories <= 400)                     .ToList(),

                DietGoal.Maintain => baseFiltered
                    .Where(p => p.Calories <= 500)
                    .ToList(),

                _ => baseFiltered             };

            var categoryPriorities = goal switch
            {
                DietGoal.Gain => new Dictionary<ProductCategory, int>
                {
                    [ProductCategory.Meat] = 6,
                    [ProductCategory.Dairy] = 5,
                    [ProductCategory.Egg] = 5,
                    [ProductCategory.Legume] = 4,
                    [ProductCategory.Grain] = 4,
                    [ProductCategory.Fish] = 3,
                    [ProductCategory.Oil] = 2,
                    [ProductCategory.Fruit] = 1,
                    [ProductCategory.Vegetable] = 1,
                    [ProductCategory.Sweet] = 0,
                    [ProductCategory.Unknown] = 0
                },
                DietGoal.Lose => new Dictionary<ProductCategory, int>
                {
                    [ProductCategory.Fish] = 5,
                    [ProductCategory.Vegetable] = 5,
                    [ProductCategory.Legume] = 4,
                    [ProductCategory.Egg] = 4,
                    [ProductCategory.Meat] = 3,
                    [ProductCategory.Dairy] = 3,
                    [ProductCategory.Grain] = 3,
                    [ProductCategory.Fruit] = 4,
                    [ProductCategory.Oil] = 1,                        [ProductCategory.Sweet] = 0,
                    [ProductCategory.Unknown] = 0
                },
                _ => new Dictionary<ProductCategory, int>                 {
                    [ProductCategory.Fish] = 5,
                    [ProductCategory.Vegetable] = 5,
                    [ProductCategory.Legume] = 4,
                    [ProductCategory.Egg] = 4,
                    [ProductCategory.Meat] = 3,
                    [ProductCategory.Dairy] = 3,
                    [ProductCategory.Grain] = 3,
                    [ProductCategory.Fruit] = 2,
                    [ProductCategory.Oil] = 1,                        [ProductCategory.Sweet] = 1,
                    [ProductCategory.Unknown] = 0
                }
            };

            var productScores = baseFiltered
                .Select(p => new
                {
                    Product = p,
                    CategoryScore = categoryPriorities.GetValueOrDefault(p.Category, 0),
                    NutrientScore = GetNutrientScore(p, goal)
                })
                .Where(x => x.NutrientScore > 0)                 .ToList();

            return productScores
                .OrderByDescending(x => x.CategoryScore * 0.4 + x.NutrientScore * 0.6)
                .Select(x => x.Product)
                .Take(60)
                .ToList();
        }
    }
}