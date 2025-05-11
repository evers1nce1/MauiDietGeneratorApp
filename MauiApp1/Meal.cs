using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{


    [Table("SavedMealPlans")]
    public class SavedMealPlan
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public double TotalCalories { get; set; }
        public double TotalProteins { get; set; }
        public double TotalFats { get; set; }
        public double TotalCarbs { get; set; }
    }

    [Table("MealPlanItems")]
    public class MealPlanItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int ProductId { get; set; }
        public string MealType { get; set; } // "Breakfast", "Lunch", "Dinner"
        public double Weight { get; set; }
    }
}
