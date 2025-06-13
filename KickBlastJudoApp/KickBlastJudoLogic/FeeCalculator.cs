using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KickBlastJudoLogic
{
    public static class FeeCalculator
    {
        private const int WeeksPerMonth = 4;
        private const double FeeBeginner = 250.00;
        private const double FeeIntermediate = 300.00;
        private const double FeeElite = 350.00;
        private const double CompetitionFee = 220.00;
        private const double CoachingRate = 90.50;
        private const int MaxCoachingHours = 5 * WeeksPerMonth;

        public static double CalculateTrainingCost(string plan)
        {
            double weekly;
            switch (plan)
            {
                case "Beginner":
                    weekly = FeeBeginner;
                    break;
                case "Intermediate":
                    weekly = FeeIntermediate;
                    break;
                case "Elite":
                    weekly = FeeElite;
                    break;
                default:
                    weekly = 0;
                    break;
            }
            return weekly * WeeksPerMonth;
        }

        public static double CalculateExtrasCost(string plan, int competitions, double hours)
        {
            double compCost = (plan == "Intermediate" || plan == "Elite")
                ? competitions * CompetitionFee
                : 0;

            if (hours > MaxCoachingHours) hours = MaxCoachingHours;
            double coachCost = hours * CoachingRate;

            return compCost + coachCost;
        }
    }
}
