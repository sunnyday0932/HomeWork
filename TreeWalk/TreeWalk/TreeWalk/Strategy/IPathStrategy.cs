namespace TreeWalk.Strategy;

public interface IPathStrategy
{
    /// <summary>
    /// 建立樹的內部快取資料
    /// </summary>
    void Build(BinaryTree tree);

    /// <summary>
    /// 查詢兩個節點之間的最短路徑
    /// </summary>
    PathResult Query(string sourceLabel, string destinationLabel);
}