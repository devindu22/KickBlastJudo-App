using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KickBlastJudoLogic
{
    public static class WeightHelper
    {
        private static readonly Dictionary<string, double> Limits =
            new Dictionary<string, double>()
        {
            { "Heavyweight",         double.PositiveInfinity },
            { "Light-Heavyweight",   100 },
            { "Middleweight",        90  },
            { "Light-Middleweight",  81  },
            { "Lightweight",         73  },
            { "Flyweight",           66  }
        };

        public static string Compare(double currentKg, string category)
        {
            double limit;
            if (!Limits.TryGetValue(category, out limit))
               return "Unknown category";

            if (double.IsInfinity(limit))
               return "No upper limit";

            if (currentKg > limit)
               return "Exceeds limit kg";

            if (Math.Abs(currentKg - limit) < 0.0001)
               return "Exactly limit kg";

               return "Below limit kg";
        }
    }
}
