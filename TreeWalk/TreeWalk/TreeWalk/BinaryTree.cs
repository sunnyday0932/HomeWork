namespace TreeWalk;

public class BinaryTree
{
    public class Node(string label, int index)
    {
        public string Label { get; } = label;
        public Node? Parent { get; internal set; }
        public Node? Left { get; internal set; }
        public Node? Right { get; internal set; }
        public int Index { get; } = index;
    }

    public Node? Root { get; }
    public IReadOnlyList<Node> Nodes { get; }
    public Dictionary<string, Node> IndexByLabel { get; }

    private BinaryTree(Node? root, List<Node> nodes, Dictionary<string, Node> dict)
    {
        Root = root;
        Nodes = nodes;
        IndexByLabel = dict;
    }

    /// <summary>
    /// 從 Level-Order CSV 建立樹
    /// </summary>
    public static BinaryTree BuildFromCsv(string csv)
    {
        var parts = csv.Split(',', StringSplitOptions.None)
            .Select(s => s?.Trim() ?? "")
            .ToArray();

        var nodes = new Node?[parts.Length];
        var dict = new Dictionary<string, Node>(StringComparer.Ordinal);

        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i])) continue;
            if (dict.ContainsKey(parts[i]))
                throw new ArgumentException($"Duplicate label {parts[i]}");

            nodes[i] = new Node(parts[i], i);
            dict.Add(parts[i], nodes[i]!);
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null) continue;
            int li = 2 * i + 1, ri = 2 * i + 2;
            if (li < nodes.Length && nodes[li] != null)
            {
                nodes[i]!.Left = nodes[li];
                nodes[li]!.Parent = nodes[i];
            }

            if (ri < nodes.Length && nodes[ri] != null)
            {
                nodes[i]!.Right = nodes[ri];
                nodes[ri]!.Parent = nodes[i];
            }
        }

        var existing = nodes.Where(n => n != null).Cast<Node>().ToList();
        return new BinaryTree(nodes[0], existing, dict);
    }
}