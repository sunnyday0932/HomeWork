namespace TreeWalk.Strategy;

/// <summary>
/// 使用「NextDirection 矩陣」策略：
/// 每一對 (src, dst) 存第一步要走的方向 (U/DL/DR)，
/// 查詢時從 source 逐步依方向走到 destination。
/// 記憶體 ~4MB，查詢距離 O(距離)，路徑 O(距離)。
/// </summary>
public class NextDirectionMatrixStrategy : IPathStrategy
{
    private BinaryTree? _tree;
    private byte[,]? _nextDirection; // [sourceIndex, destinationIndex] = 第一個方向
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

        // 初始化為 None
        for (int i = 0; i < nodeCount; i++)
        for (int j = 0; j < nodeCount; j++)
            _nextDirection[i, j] = None;

        // 為每個節點建立方向矩陣
        for (int sourceNodeIndex = 0; sourceNodeIndex < nodeCount; sourceNodeIndex++)
        {
            ComputeNextDirectionForSource(sourceNodeIndex, tree);
        }
    }

    private void ComputeNextDirectionForSource(int sourceNodeIndex, BinaryTree tree)
    {
        int nodeCount = tree.Nodes.Count;
        var visited = new bool[nodeCount];
        var queue = new Queue<int>();
        var parentIndex = new int[nodeCount];

        Array.Fill(parentIndex, -1);

        visited[sourceNodeIndex] = true;
        queue.Enqueue(sourceNodeIndex);

        while (queue.Count > 0)
        {
            int currentNodeIndex = queue.Dequeue();
            var currentNode = tree.Nodes[currentNodeIndex];

            foreach (var neighborNode in GetNeighbors(currentNode))
            {
                int neighborNodeIndex = _nodeToIndex![neighborNode];

                if (!visited[neighborNodeIndex])
                {
                    visited[neighborNodeIndex] = true;
                    parentIndex[neighborNodeIndex] = currentNodeIndex;
                    queue.Enqueue(neighborNodeIndex);

                    if (parentIndex[neighborNodeIndex] == sourceNodeIndex)
                    {
                        // 鄰居是直接相鄰：記錄方向
                        _nextDirection![sourceNodeIndex, neighborNodeIndex] =
                            EncodeDirection(tree.Nodes[sourceNodeIndex], neighborNode);
                    }
                    else
                    {
                        // 繼承父節點的第一步方向
                        _nextDirection![sourceNodeIndex, neighborNodeIndex] =
                            _nextDirection![sourceNodeIndex, parentIndex[neighborNodeIndex]];
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
        if (_tree == null || _nextDirection == null || _labelToIndex == null)
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
            byte dirCode = _nextDirection![currentIndex, destinationIndex];
            if (dirCode == None)
                throw new InvalidOperationException($"No path between {sourceLabel} and {destinationLabel}");

            var currentNode = _tree!.Nodes[currentIndex];
            var nextNode = GetNextNode(currentNode, dirCode);

            directions.Add(DecodeDirection(dirCode));
            currentIndex = _nodeToIndex![nextNode];
            stepCount++;
        }

        return new PathResult
        {
            Distance = stepCount,
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