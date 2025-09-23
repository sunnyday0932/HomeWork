using System.Text;

namespace TreeWalk;

public static class LevelOrderCsvBuilder
{
    public static string BuildLeftSkewedCsv(int depth)
        => BuildCsvByIndices(EnumerateSkewedIndices(depth, goLeftFirst: true));

    public static string BuildRightSkewedCsv(int depth)
        => BuildCsvByIndices(EnumerateSkewedIndices(depth, goLeftFirst: false));

    public static string BuildZigZagCsv(int depth, bool startLeft = true)
        => BuildCsvByIndices(EnumerateZigZagIndices(depth, startLeft));

    public static string BuildCsvFromIndexMap(IDictionary<int, string> indexToLabel)
    {
        if (indexToLabel == null || indexToLabel.Count == 0) return string.Empty;
        int maxIndex = indexToLabel.Keys.Max();
        var slots = new string[maxIndex + 1];
        for (int i = 0; i <= maxIndex; i++) slots[i] = string.Empty;
        foreach (var kv in indexToLabel) slots[kv.Key] = kv.Value ?? string.Empty;
        return string.Join(",", slots);
    }
    
    public static string BuildCompleteTreeCsv(int depth)
    {
        if (depth <= 0) return string.Empty;
        int n = (1 << depth) - 1;
        var labels = new string[n];
        for (int i = 0; i < n; i++)
            labels[i] = SpreadsheetLabel(i); // A, B, C, ..., Z, AA, AB, ...
        return string.Join(",", labels);
    }

    private static string BuildCsvByIndices(IList<int> indices)
    {
        var map = new Dictionary<int, string>();
        for (int i = 0; i < indices.Count; i++)
        {
            map[indices[i]] = Label(i); // A..J（深度<=26 可用）
        }
        return BuildCsvFromIndexMap(map);
    }

    private static IList<int> EnumerateSkewedIndices(int depth, bool goLeftFirst)
    {
        var list = new List<int>(depth);
        int idx = 0;
        for (int d = 0; d < depth; d++)
        {
            list.Add(idx);
            idx = goLeftFirst ? (2 * idx + 1) : (2 * idx + 2);
        }
        return list;
    }

    private static IList<int> EnumerateZigZagIndices(int depth, bool startLeft)
    {
        var list = new List<int>(depth);
        int idx = 0;
        bool goLeft = startLeft;
        for (int d = 0; d < depth; d++)
        {
            list.Add(idx);
            idx = goLeft ? (2 * idx + 1) : (2 * idx + 2);
            goLeft = !goLeft;
        }
        return list;
    }

    private static string Label(int i)
    {
        // A..Z, AA..；對我們測試(<=10)只會到 J
        var sb = new StringBuilder();
        i = Math.Max(i, 0);
        do
        {
            int r = i % 26;
            sb.Insert(0, (char)('A' + r));
            i = i / 26 - 1;
        } while (i >= 0);
        return sb.ToString();
    }
    
    private static string SpreadsheetLabel(int i)
    {
        var sb = new StringBuilder();
        i = Math.Max(i, 0);
        do
        {
            int r = i % 26;
            sb.Insert(0, (char)('A' + r));
            i = i / 26 - 1;
        } while (i >= 0);
        return sb.ToString();
    }
}