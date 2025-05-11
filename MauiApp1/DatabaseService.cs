using SQLite;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MauiApp1
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        public DatabaseService()
        {
            Task.Run(async () =>
            {
                await InitializeDatabase();
            }).GetAwaiter().GetResult();
        }

        private async Task InitializeDatabase()
        {
            _db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "products.db3"));
            await _db.CreateTableAsync<Product>();
            await _db.CreateTableAsync<SavedMealPlan>();
            await _db.CreateTableAsync<MealPlanItem>();

        }

        public async Task DeleteMealPlan(int planId)
        {
            await _db.DeleteAsync<SavedMealPlan>(planId);
        }

        public async Task DeleteMealPlanItems(int planId)
        {
            await _db.Table<MealPlanItem>().DeleteAsync(i => i.PlanId == planId);
        }
        public async Task<List<Product>> GetAllProducts()
        {
            return await _db.Table<Product>().ToListAsync();
        }

        public async Task<ObservableCollection<Product>> GetAllProductsAsync()
        {
            var products = await _db.Table<Product>().ToListAsync();
            return new ObservableCollection<Product>(products);
        }

        public async Task<int> AddProductAsync(Product product)
        {
            return await _db.InsertAsync(product);
        }

        public async Task<int> UpdateProductAsync(Product product)
        {
            return await _db.UpdateAsync(product);
        }

        public async Task<int> DeleteProductAsync(int id)
        {
            return await _db.DeleteAsync<Product>(id);
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _db.Table<Product>()
                          .Where(p => p.Id == id)
                          .FirstOrDefaultAsync();
        }

        public async Task<List<Product>> SearchProductsAsync(string searchTerm)
        {
            return await _db.Table<Product>()
                           .Where(p => p.Name.Contains(searchTerm))
                           .ToListAsync();
        }
        public async Task<DietOptimizer.MealPlan> GenerateDietPlan(double dailyCalories, DietOptimizer.DietGoal goal)
        {
            var allProducts = await GetAllProducts();
            var optimizer = new DietOptimizer();
            return optimizer.GenerateDietPlan(allProducts, dailyCalories, goal);
        }

        public async Task SaveMealPlan(DietOptimizer.MealPlan plan, string planName)
        {
                        await _db.CreateTableAsync<SavedMealPlan>();
            await _db.CreateTableAsync<MealPlanItem>();

            var savedPlan = new SavedMealPlan
            {
                Name = planName,
                CreatedDate = DateTime.Now,
                TotalCalories = plan.TotalCalories,
                TotalProteins = plan.TotalProteins,
                TotalFats = plan.TotalFats,
                TotalCarbs = plan.TotalCarbs
            };

            await _db.InsertAsync(savedPlan);

                        await SaveMealItems(plan.Breakfast, savedPlan.Id, "Breakfast");
            await SaveMealItems(plan.Lunch, savedPlan.Id, "Lunch");
            await SaveMealItems(plan.Dinner, savedPlan.Id, "Dinner");
        }

        private async Task SaveMealItems(List<Product> products, int planId, string mealType)
        {
            foreach (var product in products)
            {
                var item = new MealPlanItem
                {
                    PlanId = planId,
                    ProductId = product.Id,
                    MealType = mealType,
                    Weight = product.Weight
                };
                await _db.InsertAsync(item);
            }
        }

        public async Task<List<SavedMealPlan>> GetSavedPlans()
        {
            await _db.CreateTableAsync<SavedMealPlan>();
            return await _db.Table<SavedMealPlan>().ToListAsync();
        }

        public async Task<DietOptimizer.MealPlan> LoadMealPlan(int planId)
        {
            var plan = await _db.Table<SavedMealPlan>().FirstOrDefaultAsync(p => p.Id == planId);
            if (plan == null) return null;

            var items = await _db.Table<MealPlanItem>().Where(i => i.PlanId == planId).ToListAsync();

            var mealPlan = new DietOptimizer.MealPlan
            {
                TotalCalories = plan.TotalCalories,
                TotalProteins = plan.TotalProteins,
                TotalFats = plan.TotalFats,
                TotalCarbs = plan.TotalCarbs
            };

                        mealPlan.Breakfast = await LoadMealProducts(items, "Breakfast");
            mealPlan.Lunch = await LoadMealProducts(items, "Lunch");
            mealPlan.Dinner = await LoadMealProducts(items, "Dinner");

            return mealPlan;
        }

        private async Task<List<Product>> LoadMealProducts(List<MealPlanItem> items, string mealType)
        {
            var mealItems = items.Where(i => i.MealType == mealType).ToList();
            var products = new List<Product>();

            foreach (var item in mealItems)
            {
                var product = await _db.Table<Product>().FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.Weight = item.Weight;
                    products.Add(product);
                }
            }

            return products;
        }

    }
}