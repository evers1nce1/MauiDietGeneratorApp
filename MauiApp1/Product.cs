using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{

    public enum ProductCategory { Meat, Dairy, Grain, Vegetable, Fruit, Legume, Egg, Fish, Oil, Sweet, Unknown }

    public class Product : INotifyPropertyChanged
    {
        private bool _isExpanded;
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public double Calories { get; set; }

        public double Proteins { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
        public double Weight { get; set; }

        public ProductCategory Category { get; set; }  
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public double DisplayCalories => Math.Round(Calories, 1);
        public double DisplayProteins => Math.Round(Proteins, 1);
        public double DisplayFats => Math.Round(Fats, 1);
        public double DisplayCarbs => Math.Round(Carbs, 1);

    }
}
