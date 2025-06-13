using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KickBlastJudoLogic
{
    public class Athlete
    {
        public string Name { get; set; }
        public string TrainingPlan { get; set; }
        public double CurrentWeightKg { get; set; }
        public string CompetitionCategory { get; set; }
        public int CompetitionsThisMonth { get; set; }
        public double PrivateHoursRequested { get; set; }
    }
}
