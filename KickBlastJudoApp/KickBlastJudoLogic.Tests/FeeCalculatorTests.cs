using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KickBlastJudoLogic;

namespace KickBlastJudoLogic.Tests
{
    [TestClass]
    public class FeeCalculatorTests
    {
        [TestMethod]
        public void CalculateTrainingCost_Beginner_Returns250Times4()
        {
            double cost = FeeCalculator.CalculateTrainingCost("Beginner");
            Assert.AreEqual(250.00 * 4, cost, 0.001);
        }

        [TestMethod]
        public void CalculateTrainingCost_InvalidPlan_ReturnsZero()
        {
            double cost = FeeCalculator.CalculateTrainingCost("UnknownPlan");
            Assert.AreEqual(0, cost, 0.001);
        }

        [TestMethod]
        public void CalculateExtrasCost_Intermediate_WithCompetitionsAndHours()
        {
            double extras = FeeCalculator.CalculateExtrasCost("Intermediate", 2, 10);
            double expected = 2 * 220.00 + 10 * 90.50;
            Assert.AreEqual(expected, extras, 0.001);
        }

        [TestMethod]
        public void CalculateExtrasCost_Beginner_WithCompetitionsAndHours()
        {
            double extras = FeeCalculator.CalculateExtrasCost("Beginner", 3, 5);
            double expected = 0 + 5 * 90.50;
            Assert.AreEqual(expected, extras, 0.001);
        }

        [TestMethod]
        public void CalculateExtrasCost_HoursExceedMaximum_CapsAtMax()
        {
            double extras = FeeCalculator.CalculateExtrasCost("Elite", 0, 25);
            double expected = 0 + 20 * 90.50;
            Assert.AreEqual(expected, extras, 0.001);
        }
    }
}
