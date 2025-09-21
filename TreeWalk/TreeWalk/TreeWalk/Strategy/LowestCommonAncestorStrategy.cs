namespace TreeWalk.Strategy;

/// <summary>
/// 使用 LCA (Binary Lifting) 策略：
/// 預處理 parent table 與 depth，
/// 查詢距離 O(logN)，路徑 O(距離)。
/// 記憶體 ~0.1MB，最省空間。
/// </summary>
public class LowestCommonAncestorStrategy : IPathStrategy
{
    private BinaryTree? _tree;
    private Dictionary<string, int>? _labelToIndex;
    private Dictionary<BinaryTree.Node, int>? _nodeToIndex;
    private int[,]? _parent; // parent[k, i] = i 的 2^k 祖先
    private int[]? _depth; // depth[i] = 節點深度
    private BinaryTree.Node[]? _indexToNode;
    private int _maxLevel;

    public void Build(BinaryTree tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _labelToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        _nodeToIndex = new Dictionary<BinaryTree.Node, int>();

        int nodeCount = tree.Nodes.Count;
        _indexToNode = new BinaryTree.Node[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            _labelToIndex[tree.Nodes[i].Label] = i;
            _nodeToIndex[tree.Nodes[i]] = i;
            _indexToNode[i] = tree.Nodes[i];
        }

        _depth = new int[nodeCount];

        // maxLevel = ⌊log2(nodeCount)⌋
        _maxLevel = (int)Math.Ceiling(Math.Log2(Math.Max(1, nodeCount)));
        _parent = new int[_maxLevel + 1, nodeCount];

        // 初始化 parent = -1
        for (int k = 0; k <= _maxLevel; k++)
        for (int i = 0; i < nodeCount; i++)
            _parent[k, i] = -1;

        // BFS 來填 depth 與 parent[0]
        if (tree.Root != null)
        {
            var queue = new Queue<BinaryTree.Node>();
            queue.Enqueue(tree.Root);
            _depth[_nodeToIndex![tree.Root]] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentIndex = _nodeToIndex[current];

                if (current.Left != null)
                {
                    int leftIndex = _nodeToIndex[current.Left];
                    _depth[leftIndex] = _depth[currentIndex] + 1;
                    _parent[0, leftIndex] = currentIndex;
                    queue.Enqueue(current.Left);
                }

                if (current.Right != null)
                {
                    int rightIndex = _nodeToIndex[current.Right];
                    _depth[rightIndex] = _depth[currentIndex] + 1;
                    _parent[0, rightIndex] = currentIndex;
                    queue.Enqueue(current.Right);
                }
            }
        }

        // 填 binary lifting table
        for (int k = 1; k <= _maxLevel; k++)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                int midParent = _parent[k - 1, i];
                if (midParent != -1)
                {
                    _parent[k, i] = _parent[k - 1, midParent];
                }
            }
        }
    }

    public PathResult Query(string sourceLabel, string destinationLabel)
    {
        if (_tree == null || _labelToIndex == null || _nodeToIndex == null || _depth == null || _parent == null ||
            _indexToNode == null)
            throw new InvalidOperationException("Strategy not built. Call Build() first.");

        if (!_labelToIndex.TryGetValue(sourceLabel, out int u))
            throw new ArgumentException($"Source label '{sourceLabel}' not found.");
        if (!_labelToIndex.TryGetValue(destinationLabel, out int v))
            throw new ArgumentException($"Destination label '{destinationLabel}' not found.");

        if (u == v)
        {
            return new PathResult
            {
                Distance = 0,
                Directions = Array.Empty<string>()
            };
        }

        int lca = GetLowestCommonAncestor(u, v);
        int distance = _depth[u] + _depth[v] - 2 * _depth[lca];

        var directions = new List<string>();

        // u → lca （一路 U）
        int cur = u;
        while (cur != lca)
        {
            directions.Add("U");
            cur = _parent![0, cur];
        }

        // v → lca （一路 U），再反轉成 lca → v
        var stack = new Stack<string>();
        cur = v;
        while (cur != lca)
        {
            int parentIndex = _parent![0, cur];
            var parentNode = _indexToNode[parentIndex];
            var curNode = _indexToNode[cur];

            if (parentNode.Left == curNode) stack.Push("DL");
            else if (parentNode.Right == curNode) stack.Push("DR");
            else throw new InvalidOperationException("Invalid parent-child relation");

            cur = parentIndex;
        }

        directions.AddRange(stack);

        return new PathResult
        {
            Distance = distance,
            Directions = directions
        };
    }

    private int GetLowestCommonAncestor(int u, int v)
    {
        if (_depth![u] < _depth[v])
            (u, v) = (v, u);

        // 把 u 提升到和 v 同深度
        int diff = _depth[u] - _depth[v];
        for (int k = 0; diff > 0; k++, diff >>= 1)
        {
            if ((diff & 1) != 0)
                u = _parent![k, u];
        }

        if (u == v) return u;

        // 從高位往下比，直到找到最低共同祖先
        for (int k = _maxLevel; k >= 0; k--)
        {
            if (_parent![k, u] != _parent[k, v])
            {
                u = _parent[k, u];
                v = _parent[k, v];
            }
        }

        return _parent![0, u];
    }
}