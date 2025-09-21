using BenchmarkDotNet.Attributes;
using TreeWalk.Strategy;

namespace TreeWalk;

public class StrategyBenchmarks
{
    private readonly BinaryTree _tree = BinaryTree.BuildFromCsv("A,B,C,,D,E,F,,,,,,,G");
    private readonly List<IPathStrategy> _strategies;
    
    public StrategyBenchmarks()
    {
        _strategies = new()
        {
            new NextHopMatrixStrategy(),
            new NextDirectionMatrixStrategy(),
            new NextDirectionWithDistanceStrategy(),
            new LowestCommonAncestorStrategy ()
        };
        foreach (var s in _strategies) s.Build(_tree);
    }

    [Benchmark]
    [Arguments("B", "F")]
    public int Query_NextHop(string src, string dst)
        => _strategies[0].Query(src, dst).Distance;

    [Benchmark]
    [Arguments("B", "F")]
    public int Query_NextDir(string src, string dst)
        => _strategies[1].Query(src, dst).Distance;

    [Benchmark]
    [Arguments("B", "F")]
    public int Query_NextDirDist(string src, string dst)
        => _strategies[2].Query(src, dst).Distance;

    [Benchmark]
    [Arguments("B", "F")]
    public int Query_LCA(string src, string dst)
        => _strategies[3].Query(src, dst).Distance;
}