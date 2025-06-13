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
    public class WeightHelperTests
    {
        [TestMethod]
        public void Compare_Heavyweight_ReturnsNoUpperLimit()
        {
            string status = WeightHelper.Compare(120, "Heavyweight");
            Assert.AreEqual("No upper limit", status);
        }

        [TestMethod]
        public void Compare_ExactLimit_ReturnsExactlyMessage()
        {
            // Assuming category "Lightweight" limit is 73
            string status = WeightHelper.Compare(73, "Lightweight");
            Assert.IsTrue(status.Contains("Exactly"));
        }

        [TestMethod]
        public void Compare_AboveLimit_ReturnsExceedsMessage()
        {
            string status = WeightHelper.Compare(80, "Lightweight"); // 80 > 73
            Assert.IsTrue(status.Contains("Exceeds"));
        }

        [TestMethod]
        public void Compare_BelowLimit_ReturnsBelowMessage()
        {
            string status = WeightHelper.Compare(70, "Lightweight"); // 70 < 73
            Assert.IsTrue(status.Contains("Below"));
        }

        [TestMethod]
        public void Compare_UnknownCategory_ReturnsUnknownCategory()
        {
            string status = WeightHelper.Compare(70, "Nonexistent");
            Assert.AreEqual("Unknown category", status);
        }
    }
}
