using TreeWalk.Strategy;

namespace TreeWalk;

public class PathStrategyTests
{
    [Theory]
    [MemberData(nameof(TreeTestCases.GetCases), MemberType = typeof(TreeTestCases))]
    public void Should_FindShortestPath(
        string csv, string source, string dest,
        int expectedDistance, string[] expectedPath, string summary)
    {
        // 可以跑四種策略都驗證
        var strategies = new IPathStrategy[]
        {
            new NextHopMatrixStrategy(),
            new NextDirectionMatrixStrategy(),
            new NextDirectionWithDistanceStrategy(),
            new LowestCommonAncestorStrategy ()
        };

        var tree = BinaryTree.BuildFromCsv(csv);

        foreach (var strategy in strategies)
        {
            strategy.Build(tree);
            var result = strategy.Query(source, dest);

            Assert.Equal(expectedDistance, result.Distance);
            Assert.Equal(expectedPath, result.Directions);
        }
        
        // 顯示 summary，方便人檢視
        System.Diagnostics.Debug.WriteLine(summary);
    }
}