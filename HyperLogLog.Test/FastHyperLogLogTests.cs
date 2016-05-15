using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.Test
{
    [TestClass]
    public class FastHyperLogLogTests
    {
        private Stopwatch stopwatch;

        [TestInitialize]
        public void Init()
        {
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.stopwatch.Stop();
            Console.WriteLine("Total test time: {0}", this.stopwatch.Elapsed);
        }

        [TestMethod]
        public void Test()
        {
            
        }


    }
}
