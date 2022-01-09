// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using HyperLogLog.BenchMark;

//HyperLogLogTests.Run();
//var summary = BenchmarkRunner.Run<GetSigmaBench>();
var summary = BenchmarkRunner.Run<Count14Bench>();
