// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using TreeWalforBenchmark;

_ = BenchmarkRunner.Run<StrategyBenchmarks>();