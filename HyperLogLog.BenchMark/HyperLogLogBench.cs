using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.BenchMark
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, targetCount: 1)]
    [RPlotExporter, RankColumn]
    public class HyperLogLogBench
    {

    }
}
