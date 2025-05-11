using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1
{
    public static class Utils
    {
        public static double CalcBaseMetabolism(double weight, double height, double age, bool isMale = true)
        {
            if (weight <= 0 || height <= 0 || age <= 0)
                throw new ArgumentException("Некорректные параметры для расчета метаболизма");
            // формула Миффлина-Сан Жеора
                double bmr;
                if (isMale)
                {
                    bmr = 10 * weight + 6.25 * height - 5 * age + 5;
                }
                else
                {
                    bmr = 10 * weight + 6.25 * height - 5 * age - 161;
                }


                return Math.Round(bmr, 1);
        }
    }
}
