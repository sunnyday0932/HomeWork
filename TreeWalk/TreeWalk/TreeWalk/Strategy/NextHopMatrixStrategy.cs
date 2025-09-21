namespace TreeWalk.Strategy;

/// <summary>
/// 使用「NextHop 矩陣」策略：
/// 為每對 (source, destination) 保存第一步要走到的「下一個節點索引」。
/// 建表流程：對每個 source 先做完整 BFS，取得 parentIndex[]，
/// 再對每個 reachable 的 destination 回溯找出第一步並填表（避免推導順序問題）。
/// 記憶體 ~8MB，查詢距離 O(距離)，路徑 O(距離)。
/// </summary>
public sealed class NextHopMatrixStrategy : IPathStrategy
{
    private BinaryTree? _tree;
    private short[,]? _nextHop; // [sourceIndex, destinationIndex] = 下一步節點索引
    private Dictionary<string, int>? _labelToIndex;
    private Dictionary<BinaryTree.Node, int>? _nodeToIndex;

    public void Build(BinaryTree tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _labelToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        _nodeToIndex = new Dictionary<BinaryTree.Node, int>();

        for (int i = 0; i < tree.Nodes.Count; i++)
        {
            _labelToIndex[tree.Nodes[i].Label] = i;
            _nodeToIndex[tree.Nodes[i]] = i;
        }

        int nodeCount = tree.Nodes.Count;
        _nextHop = new short[nodeCount, nodeCount];

        // 先設成 -1（未知/不可達）
        for (int i = 0; i < nodeCount; i++)
            for (int j = 0; j < nodeCount; j++)
                _nextHop[i, j] = -1;

        // 對每個節點做一次 BFS，然後用 parentIndex 回溯填入第一步
        for (int sourceNodeIndex = 0; sourceNodeIndex < nodeCount; sourceNodeIndex++)
        {
            FillFirstHopTableForSource(sourceNodeIndex, tree);
        }
    }

    /// <summary>
    /// 對指定 source 先做完整 BFS，拿到 parentIndex 與 visited；
    /// 再對每個 reachable 的 destination，回溯到 source 找到第一步，填入 nextHop[source, destination]。
    /// </summary>
    private void FillFirstHopTableForSource(int sourceNodeIndex, BinaryTree tree)
    {
        int nodeCount = tree.Nodes.Count;
        var parentIndexOf = new int[nodeCount];   // parentIndexOf[x] = x 在 BFS 樹中的父節點索引
        var wasVisited = new bool[nodeCount];

        Array.Fill(parentIndexOf, -1);

        // --- BFS（無向：父、左、右）---
        var queue = new Queue<int>();
        wasVisited[sourceNodeIndex] = true;
        queue.Enqueue(sourceNodeIndex);

        while (queue.Count > 0)
        {
            int currentNodeIndex = queue.Dequeue();
            var currentNode = tree.Nodes[currentNodeIndex];

            foreach (var neighbor in EnumerateUndirectedNeighbors(currentNode))
            {
                int neighborIndex = _nodeToIndex![neighbor];
                if (wasVisited[neighborIndex]) continue;

                wasVisited[neighborIndex] = true;
                parentIndexOf[neighborIndex] = currentNodeIndex;
                queue.Enqueue(neighborIndex);
            }
        }

        // --- 用 parentIndex 回溯，為每個 reachable 的 destination 找出「第一步」---
        for (int destinationIndex = 0; destinationIndex < nodeCount; destinationIndex++)
        {
            if (destinationIndex == sourceNodeIndex) continue;
            if (!wasVisited[destinationIndex]) continue; // 不可達（理論上樹一定可達，但保險判斷）

            short firstHopIndex = FindFirstHopFromSourceToDestination(
                sourceNodeIndex, destinationIndex, parentIndexOf);

            _nextHop![sourceNodeIndex, destinationIndex] = firstHopIndex;
        }
    }

    /// <summary>
    /// 從 destination 透過 parentIndex 一路回溯到 source；
    /// 回溯過程中最後一個「尚未到達 source 前」的節點，就是 source 的第一步。
    /// 例：source=A, path: A -> B -> C -> D（destination）
    /// 回溯：D -> C -> B -> A；第一步為 B。
    /// </summary>
    private short FindFirstHopFromSourceToDestination(
        int sourceNodeIndex,
        int destinationNodeIndex,
        int[] parentIndexOf)
    {
        int cur = destinationNodeIndex;
        int parent = parentIndexOf[cur];

        // 若 destination 就是 source 的直鄰，parent 會是 source
        // 否則一路回溯直到 parent == source
        while (parent != sourceNodeIndex)
        {
            // 若意外找不到 source（防禦式檢查）
            if (parent == -1)
                return -1;

            cur = parent;
            parent = parentIndexOf[cur];
        }

        return (short)cur; // cur 即為 source 的第一步
    }

    private IEnumerable<BinaryTree.Node> EnumerateUndirectedNeighbors(BinaryTree.Node node)
    {
        if (node.Parent != null) yield return node.Parent;
        if (node.Left != null) yield return node.Left;
        if (node.Right != null) yield return node.Right;
    }

    public PathResult Query(string sourceLabel, string destinationLabel)
    {
        if (_tree == null || _nextHop == null || _labelToIndex == null || _nodeToIndex == null)
            throw new InvalidOperationException("Strategy not built. Call Build() first.");

        if (!_labelToIndex.TryGetValue(sourceLabel, out int sourceIndex))
            throw new ArgumentException($"Source label '{sourceLabel}' not found.");
        if (!_labelToIndex.TryGetValue(destinationLabel, out int destinationIndex))
            throw new ArgumentException($"Destination label '{destinationLabel}' not found.");

        if (sourceIndex == destinationIndex)
        {
            return new PathResult
            {
                Distance = 0,
                Directions = Array.Empty<string>()
            };
        }

        var directions = new List<string>();
        int currentIndex = sourceIndex;
        int stepCount = 0;

        while (currentIndex != destinationIndex)
        {
            int nextIndex = _nextHop![currentIndex, destinationIndex];
            if (nextIndex == -1)
                throw new InvalidOperationException($"No path between {sourceLabel} and {destinationLabel}");

            directions.Add(GetDirection(_tree!.Nodes[currentIndex], _tree.Nodes[nextIndex]));
            currentIndex = nextIndex;
            stepCount++;

            // 防護：避免理論外的循環
            if (stepCount > _tree.Nodes.Count)
                throw new InvalidOperationException("Detected unexpected loop while following nextHop.");
        }

        return new PathResult
        {
            Distance = stepCount,
            Directions = directions
        };
    }

    private string GetDirection(BinaryTree.Node fromNode, BinaryTree.Node toNode)
    {
        if (fromNode.Parent == toNode) return "U";
        if (fromNode.Left == toNode) return "DL";
        if (fromNode.Right == toNode) return "DR";
        throw new InvalidOperationException("Invalid edge between nodes");
    }
}
