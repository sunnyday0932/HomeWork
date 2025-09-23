using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using TreeWalk;
using TreeWalk.Strategy;

namespace TreeWalforBenchmark;

[MemoryDiagnoser]
[SimpleJob(runtimeMoniker: RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 5)]
public class StrategyBenchmarks
{
    private const int Depth = 10; // 10 層（節點數 2^10 - 1 = 1023）
    private string _csv = null!;
    private BinaryTree _tree = null!;

    // 查詢點位（用索引推標籤，避免依賴命名規則）
    private string _rootLabel = null!;
    private string _leftmostLeafLabel = null!;
    private string _rightmostLeafLabel = null!;
    private string _midLeftLabel = null!;
    private string _midRightLabel = null!;

    // 已建好的策略，用於「查詢」基準
    private readonly IPathStrategy _nextHop = new NextHopMatrixStrategy();
    private readonly IPathStrategy _nextDir = new NextDirectionMatrixStrategy();
    private readonly IPathStrategy _nextDirDist = new NextDirectionWithDistanceStrategy();
    private readonly IPathStrategy _lca = new LowestCommonAncestorStrategy();

    [GlobalSetup]
    public void GlobalSetup()
    {
        _csv = LevelOrderCsvBuilder.BuildCompleteTreeCsv(Depth);
        _tree = BinaryTree.BuildFromCsv(_csv);

        // 幾個代表性節點
        int root = 0;
        int leftmostLeaf = (1 << (Depth - 1)) - 1; // 最底層最左
        int rightmostLeaf = (1 << Depth) - 2; // 最底層最右
        int midLeft = (1 << (Depth - 2)) - 1; // 倒數第二層最左
        int midRight = (1 << (Depth - 1)) - 2; // 倒數第一層靠右一點

        _rootLabel = _tree.Nodes[root].Label;
        _leftmostLeafLabel = _tree.Nodes[leftmostLeaf].Label;
        _rightmostLeafLabel = _tree.Nodes[rightmostLeaf].Label;
        _midLeftLabel = _tree.Nodes[midLeft].Label;
        _midRightLabel = _tree.Nodes[midRight].Label;

        // 為查詢基準準備「已 Build 完」的策略
        _nextHop.Build(_tree);
        _nextDir.Build(_tree);
        _nextDirDist.Build(_tree);
        _lca.Build(_tree);
    }

    // ---------------------------
    // 建表（Build）效能 & 記憶體
    // ---------------------------

    [Benchmark(Description = "Build NextHopMatrix")]
    public void Build_NextHopMatrix()
    {
        var s = new NextHopMatrixStrategy();
        s.Build(_tree);
    }

    [Benchmark(Description = "Build NextDirectionMatrix")]
    public void Build_NextDirectionMatrix()
    {
        var s = new NextDirectionMatrixStrategy();
        s.Build(_tree);
    }

    [Benchmark(Description = "Build NextDirectionWithDistance")]
    public void Build_NextDirectionWithDistance()
    {
        var s = new NextDirectionWithDistanceStrategy();
        s.Build(_tree);
    }

    [Benchmark(Description = "Build LowestCommonAncestor")]
    public void Build_LCA()
    {
        var s = new LowestCommonAncestorStrategy();
        s.Build(_tree);
    }

    // ---------------------------
    // 查詢（Query）效能
    // ---------------------------

    // 1) 根 → 最左葉（距離 = Depth - 1 = 9）
    [Benchmark(Description = "Query NextHop: root->leftmostLeaf")]
    public int Query_NextHop_Root_To_LeftmostLeaf()
        => _nextHop.Query(_rootLabel, _leftmostLeafLabel).Distance;

    [Benchmark(Description = "Query NextDir: root->leftmostLeaf")]
    public int Query_NextDir_Root_To_LeftmostLeaf()
        => _nextDir.Query(_rootLabel, _leftmostLeafLabel).Distance;

    [Benchmark(Description = "Query NextDir+Dist: root->leftmostLeaf")]
    public int Query_NextDirDist_Root_To_LeftmostLeaf()
        => _nextDirDist.Query(_rootLabel, _leftmostLeafLabel).Distance;

    [Benchmark(Description = "Query LCA: root->leftmostLeaf")]
    public int Query_LCA_Root_To_LeftmostLeaf()
        => _lca.Query(_rootLabel, _leftmostLeafLabel).Distance;

    // 2) 最長路徑：最左葉 ↔ 最右葉（距離 = 2*(Depth-1) = 18）
    [Benchmark(Description = "Query NextHop: leftmostLeaf->rightmostLeaf")]
    public int Query_NextHop_Leftmost_To_Rightmost()
        => _nextHop.Query(_leftmostLeafLabel, _rightmostLeafLabel).Distance;

    [Benchmark(Description = "Query NextDir: leftmostLeaf->rightmostLeaf")]
    public int Query_NextDir_Leftmost_To_Rightmost()
        => _nextDir.Query(_leftmostLeafLabel, _rightmostLeafLabel).Distance;

    [Benchmark(Description = "Query NextDir+Dist: leftmostLeaf->rightmostLeaf")]
    public int Query_NextDirDist_Leftmost_To_Rightmost()
        => _nextDirDist.Query(_leftmostLeafLabel, _rightmostLeafLabel).Distance;

    [Benchmark(Description = "Query LCA: leftmostLeaf->rightmostLeaf")]
    public int Query_LCA_Leftmost_To_Rightmost()
        => _lca.Query(_leftmostLeafLabel, _rightmostLeafLabel).Distance;

    // 3) 中層到中層（常見情境）
    [Benchmark(Description = "Query NextHop: midLeft->midRight")]
    public int Query_NextHop_MidLeft_To_MidRight()
        => _nextHop.Query(_midLeftLabel, _midRightLabel).Distance;

    [Benchmark(Description = "Query NextDir: midLeft->midRight")]
    public int Query_NextDir_MidLeft_To_MidRight()
        => _nextDir.Query(_midLeftLabel, _midRightLabel).Distance;

    [Benchmark(Description = "Query NextDir+Dist: midLeft->midRight")]
    public int Query_NextDirDist_MidLeft_To_MidRight()
        => _nextDirDist.Query(_midLeftLabel, _midRightLabel).Distance;

    [Benchmark(Description = "Query LCA: midLeft->midRight")]
    public int Query_LCA_MidLeft_To_MidRight()
        => _lca.Query(_midLeftLabel, _midRightLabel).Distance;
}