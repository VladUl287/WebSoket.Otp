using System.Text;

namespace WebSockets.Otp.Core.Utils;

public static class CSharpTrieCodeGenerator
{
    private sealed class Value(byte[] bytes, int index)
    {
        public byte[] Bytes { get; } = bytes; 
        public int Index { get; } = index;
    }

    public static string GenerateTrie(byte[][] values)
    {
        var mapped = values
            .Select((b, i) => new Value(b, i))
            .ToList();

        var body = new StringBuilder();
        body.Append(Build(mapped, 0));
        body.Append("return -1;");

        var sb = new StringBuilder();
        sb.Append("public static class GeneratedTrie{public static int Resolve(byte[] a, int i){");
        sb.Append(body);
        sb.Append("}}");
        return sb.ToString();
    }

    private static string Build(List<Value> values, int depth)
    {
        values.Sort((x, y) => y.Bytes.Length.CompareTo(x.Bytes.Length));

        var d = depth;
        var count = values.Count;

        if (count == 0) return "return -1;";
        if (count == 1 && d >= values[0].Bytes.Length)
            return $"return {values[0].Index};";

        if (count == 1)
        {
            var v = values[0];
            var b = v.Bytes;
            var chunks = new List<string>();

            var i = d;
            while (i < b.Length - 4)
            {
                byte a1 = b[i], a2 = b[i + 1], a3 = b[i + 2], a4 = b[i + 3];

                int packValue = unchecked(a1 | (a2 << 8) | (a3 << 16) | (a4 << 24));
                chunks.Add($"((a[i+{i}] | (a[i+{i + 1}] << 8) | (a[i+{i + 2}] << 16) | (a[i+{i + 3}] << 24)) == {packValue})");
                i += 4;
            }

            while (i < b.Length)
            {
                chunks.Add($"a[i+{i}] == {b[i]}");
                i++;
            }

            return $"return ({string.Join(" && ", chunks)}) ? {v.Index} : -1;";
        }

        bool canPack4 = values.All(c => (c.Bytes.Length - d) > 4);
        bool canPack3 = values.All(c => (c.Bytes.Length - d) > 3);

        var map = new Dictionary<int, List<Value>>();

        if (canPack4)
        {
            foreach (var value in values)
            {
                var b = value.Bytes;

                if (d + 3 < b.Length)
                {
                    int packed = unchecked(b[d] | (b[d + 1] << 8) | (b[d + 2] << 16) | (b[d + 3] << 24));
                    if (!map.TryGetValue(packed, out var list))
                    {
                        list = [];
                        map[packed] = list;
                    }
                    list.Add(value);
                    continue;
                }

                if (d < b.Length)
                {
                    int by = b[d];
                    if (!map.TryGetValue(by, out var list))
                    {
                        list = [];
                        map[by] = list;
                    }
                    list.Add(value);
                }
            }

            var sb = new StringBuilder();
            sb.Append($"switch (a[i+{d}] | (a[i+{d + 1}] << 8) | (a[i+{d + 2}] << 16) | (a[i+{d + 3}] << 24)) {{");
            foreach (var kv in map)
            {
                sb.Append($"case {kv.Key}: {{");
                sb.Append(Build(kv.Value, d + 4));
                sb.Append('}');
            }
            sb.Append("default: return -1;}");
            return sb.ToString();
        }
        else if (canPack3)
        {
            foreach (var value in values)
            {
                var b = value.Bytes;

                if (d + 2 < b.Length)
                {
                    int packed = unchecked(b[d] | (b[d + 1] << 8) | (b[d + 2] << 16));
                    if (!map.TryGetValue(packed, out var list))
                    {
                        list = [];
                        map[packed] = list;
                    }
                    list.Add(value);
                    continue;
                }

                if (d < b.Length)
                {
                    int by = b[d];
                    if (!map.TryGetValue(by, out var list))
                    {
                        list = [];
                        map[by] = list;
                    }
                    list.Add(value);
                }
            }

            var sb = new StringBuilder();
            sb.Append($"switch (a[i+{d}] | (a[i+{d + 1}] << 8) | (a[i+{d + 2}] << 16)) {{");
            foreach (var kv in map)
            {
                sb.Append($"case {kv.Key}: {{");
                sb.Append(Build(kv.Value, d + 4));
                sb.Append('}');
            }
            sb.Append("default: return -1;}");
            return sb.ToString();
        }
        else
        {
            int defaultIndex = -1;

            foreach (var value in values)
            {
                var b = value.Bytes;

                if (d < b.Length)
                {
                    int by = b[d];
                    if (!map.TryGetValue(by, out var list))
                    {
                        list = new List<Value>();
                        map[by] = list;
                    }
                    list.Add(value);
                    continue;
                }

                defaultIndex = value.Index;
            }

            var sb = new StringBuilder();
            sb.Append($"switch (a[i+{d}]) {{");
            foreach (var kv in map)
            {
                sb.Append($"case {kv.Key}: {{");
                sb.Append(Build(kv.Value, d + 1));
                sb.Append('}');
            }
            sb.Append($"default: return {defaultIndex};}}");
            return sb.ToString();
        }
    }
}
