namespace TreeWalk.Strategy;

/// <summary>
/// 使用「NextDirection + Distance 矩陣」策略：
/// 每一對 (src, dst) 存第一步方向與距離。
/// 查詢距離 O(1)，路徑 O(距離)。
/// 記憶體 ~8MB。
/// </summary>
public class NextDirectionWithDistanceStrategy : IPathStrategy
{
    private BinaryTree? _tree;
    private byte[,]? _nextDirection; // [sourceIndex, destinationIndex] = 第一個方向
    private byte[,]? _distance;      // [sourceIndex, destinationIndex] = 距離
    private Dictionary<string, int>? _labelToIndex;
    private Dictionary<BinaryTree.Node, int>? _nodeToIndex;

    private const byte None = 0;
    private const byte Up = 1;
    private const byte DownLeft = 2;
    private const byte DownRight = 3;

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
        _nextDirection = new byte[nodeCount, nodeCount];
        _distance = new byte[nodeCount, nodeCount];

        // 初始化
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = 0; j < nodeCount; j++)
            {
                _nextDirection[i, j] = None;
                _distance[i, j] = 0;
            }
        }

        // 為每個節點做 BFS
        for (int sourceIndex = 0; sourceIndex < nodeCount; sourceIndex++)
        {
            ComputeNextDirectionAndDistanceForSource(sourceIndex, tree);
        }
    }

    private void ComputeNextDirectionAndDistanceForSource(int sourceIndex, BinaryTree tree)
    {
        int nodeCount = tree.Nodes.Count;
        var visited = new bool[nodeCount];
        var queue = new Queue<int>();
        var parentIndex = new int[nodeCount];

        Array.Fill(parentIndex, -1);

        visited[sourceIndex] = true;
        queue.Enqueue(sourceIndex);

        while (queue.Count > 0)
        {
            int currentIndex = queue.Dequeue();
            var currentNode = tree.Nodes[currentIndex];

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                int neighborIndex = _nodeToIndex![neighbor];
                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    parentIndex[neighborIndex] = currentIndex;
                    queue.Enqueue(neighborIndex);

                    // 設定距離
                    _distance![sourceIndex, neighborIndex] =
                        (byte)(_distance![sourceIndex, currentIndex] + 1);

                    // 設定第一步方向
                    if (parentIndex[neighborIndex] == sourceIndex)
                    {
                        _nextDirection![sourceIndex, neighborIndex] =
                            EncodeDirection(tree.Nodes[sourceIndex], neighbor);
                    }
                    else
                    {
                        _nextDirection![sourceIndex, neighborIndex] =
                            _nextDirection![sourceIndex, parentIndex[neighborIndex]];
                    }
                }
            }
        }
    }

    private IEnumerable<BinaryTree.Node> GetNeighbors(BinaryTree.Node node)
    {
        if (node.Parent != null) yield return node.Parent;
        if (node.Left != null) yield return node.Left;
        if (node.Right != null) yield return node.Right;
    }

    public PathResult Query(string sourceLabel, string destinationLabel)
    {
        if (_tree == null || _nextDirection == null || _distance == null || _labelToIndex == null)
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

        while (currentIndex != destinationIndex)
        {
            byte dirCode = _nextDirection![currentIndex, destinationIndex];
            if (dirCode == None)
                throw new InvalidOperationException($"No path between {sourceLabel} and {destinationLabel}");

            var currentNode = _tree!.Nodes[currentIndex];
            var nextNode = GetNextNode(currentNode, dirCode);

            directions.Add(DecodeDirection(dirCode));
            currentIndex = _nodeToIndex![nextNode];
        }

        return new PathResult
        {
            Distance = _distance![sourceIndex, destinationIndex],
            Directions = directions
        };
    }

    private byte EncodeDirection(BinaryTree.Node fromNode, BinaryTree.Node toNode)
    {
        if (fromNode.Parent == toNode) return Up;
        if (fromNode.Left == toNode) return DownLeft;
        if (fromNode.Right == toNode) return DownRight;
        throw new InvalidOperationException("Invalid edge between nodes");
    }

    private string DecodeDirection(byte dirCode) => dirCode switch
    {
        Up => "U",
        DownLeft => "DL",
        DownRight => "DR",
        _ => throw new InvalidOperationException("Invalid direction code")
    };

    private BinaryTree.Node GetNextNode(BinaryTree.Node fromNode, byte dirCode) => dirCode switch
    {
        Up => fromNode.Parent ?? throw new InvalidOperationException(),
        DownLeft => fromNode.Left ?? throw new InvalidOperationException(),
        DownRight => fromNode.Right ?? throw new InvalidOperationException(),
        _ => throw new InvalidOperationException()
    };
}