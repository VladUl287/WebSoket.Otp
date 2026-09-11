using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;

namespace WebSockets.Otp.Core.Utils;

public static class TrieGenerator
{
    private sealed class Value
    {
        public required byte[] Bytes;
        public required int Index;
    }

    public delegate int SpanLookup(ReadOnlySpan<byte> input);

    public static SpanLookup GenerateTrie(byte[][] values)
    {
        var mapped = values
            .Select((b, i) => new Value { Bytes = b, Index = i })
            .ToList();

        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TrieAsm"), AssemblyBuilderAccess.Run);
        var mod = asm.DefineDynamicModule("TrieModule");
        var tb = mod.DefineType("Trie", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);

        var mb = tb.DefineMethod("Resolve", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(ReadOnlySpan<byte>)]);

        var il = mb.GetILGenerator();
        var returnLabel = il.DefineLabel();

        Build(il, mapped, depth: 0);

        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ret);

        var type = tb.CreateType();
        var method = type.GetMethod("Resolve") ?? throw new NullReferenceException();

        return method.CreateDelegate<SpanLookup>();
    }

    private static void Build(ILGenerator il, List<Value> values, int depth)
    {
        var d = depth;
        var count = values.Count;

        if (count == 0)
        {
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Ret);
            return;
        }

        if (count == 1 && d >= values[0].Bytes.Length)
        {
            il.Emit(OpCodes.Ldc_I4, values[0].Index);
            il.Emit(OpCodes.Ret);
            return;
        }

        if (count == 1)
        {
            EmitSingleValueCheck(il, values[0], d);
            return;
        }

        var canPack4 = values.All(v => v.Bytes.Length - d > 4);
        var canPack3 = values.All(v => v.Bytes.Length - d > 3);

        if (canPack4)
            EmitPack4(il, values, d);
        else if (canPack3)
            EmitPack3(il, values, d);
        else
            EmitByteSwitch(il, values, d);
    }

    private static void EmitSingleValueCheck(ILGenerator il, Value v, int d)
    {
        var b = v.Bytes.AsSpan();
        var failLabel = il.DefineLabel();

        var i = d;

        while (i < b.Length - 8)
        {
            EmitLoadPacked8(il, i);

            var packed = BinaryPrimitives.ReadInt64LittleEndian(b[i..]);
            il.Emit(OpCodes.Ldc_I4, packed);
            il.Emit(OpCodes.Bne_Un, failLabel);
            i += 4;
        }

        while (i < b.Length - 4)
        {
            EmitLoadPacked4(il, i);

            var packed = BinaryPrimitives.ReadInt32LittleEndian(b[i..]);
            il.Emit(OpCodes.Ldc_I4, packed);
            il.Emit(OpCodes.Bne_Un, failLabel);
            i += 4;
        }

        while (i < b.Length)
        {
            EmitLoadByte(il, i);
            il.Emit(OpCodes.Ldc_I4, (int)b[i]);
            il.Emit(OpCodes.Bne_Un, failLabel);
            i++;
        }

        // All comparisons passed → return index
        il.Emit(OpCodes.Ldc_I4, v.Index);
        il.Emit(OpCodes.Ret);

        // Any comparison failed → return -1
        il.MarkLabel(failLabel);
        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitPack4(ILGenerator il, List<Value> values, int d)
    {
        var map = new Dictionary<int, List<Value>>();

        foreach (var v in values)
        {
            var b = v.Bytes;

            var packed = BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(d));
            if (!map.TryGetValue(packed, out var list))
                map[packed] = list = [];

            list.Add(v);
        }

        EmitPackedSwitch(il, map, d, packBytes: 4, nextDepth: d + 4);
    }

    private static void EmitPack3(ILGenerator il, List<Value> values, int d)
    {
        var map = new Dictionary<int, List<Value>>();

        foreach (var v in values)
        {
            var b = v.Bytes;
            if (d + 2 < b.Length)
            {
                var packed = b[d] | b[d + 1] << 8 | b[d + 2] << 16;

                if (!map.TryGetValue(packed, out var list))
                    map[packed] = list = [];

                list.Add(v);
            }
            else if (d < b.Length)
            {
                var key = b[d];

                if (!map.TryGetValue(key, out var list))
                    map[key] = list = [];

                list.Add(v);
            }
        }

        EmitPackedSwitch(il, map, d, packBytes: 3, nextDepth: d + 3);
    }

    private static void EmitByteSwitch(ILGenerator il, List<Value> values, int d)
    {
        var map = new Dictionary<int, List<Value>>();
        var defaultIndex = -1;

        foreach (var v in values)
        {
            var b = v.Bytes;
            if (d < b.Length)
            {
                var key = b[d];

                if (!map.TryGetValue(key, out var list))
                    map[key] = list = [];

                list.Add(v);
            }
            else
            {
                defaultIndex = v.Index;
            }
        }

        EmitLoadByte(il, d);

        var labels = map.Keys.Select(_ => il.DefineLabel()).ToArray();
        var defaultLabel = il.DefineLabel();

        // comparison chain
        var keys = map.Keys.ToArray();
        for (int k = 0; k < keys.Length; k++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, keys[k]);
            il.Emit(OpCodes.Beq, labels[k]);
        }
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Br, defaultLabel);

        int idx = 0;
        foreach (var kv in map)
        {
            il.MarkLabel(labels[idx++]);
            Build(il, kv.Value, d + 1);
        }

        il.MarkLabel(defaultLabel);
        il.Emit(OpCodes.Ldc_I4, defaultIndex);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitPackedSwitch(
        ILGenerator il,
        Dictionary<int, List<Value>> map,
        int d,
        int packBytes,
        int nextDepth)
    {
        if (packBytes == 4) EmitLoadPacked4(il, d);
        else EmitLoadPacked3(il, d);

        var keys = map.Keys.ToArray();
        var labels = keys.Select(_ => il.DefineLabel()).ToArray();
        var defaultLabel = il.DefineLabel();

        for (int k = 0; k < keys.Length; k++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, keys[k]);
            il.Emit(OpCodes.Beq, labels[k]);
        }
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Br, defaultLabel);

        for (int k = 0; k < keys.Length; k++)
        {
            il.MarkLabel(labels[k]);
            Build(il, map[keys[k]], nextDepth);
        }

        il.MarkLabel(defaultLabel);
        il.Emit(OpCodes.Ldc_I4_M1);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitLoadByte(ILGenerator il, int offset)
    {
        il.Emit(OpCodes.Ldarg_0);           // a
        il.Emit(OpCodes.Ldarg_1);           // i
        if (offset != 0)
        {
            il.Emit(OpCodes.Ldc_I4, offset);
            il.Emit(OpCodes.Add);
        }
        il.Emit(OpCodes.Ldelem_U1);         // a[i+offset]  (zero-extended)
    }


    private static readonly MethodInfo ReadInt64LE =
        typeof(BinaryPrimitives).GetMethod(nameof(BinaryPrimitives.ReadInt64LittleEndian), [typeof(ReadOnlySpan<byte>)])!;

    private static readonly MethodInfo SpanSlice =
        typeof(ReadOnlySpan<byte>).GetMethod(nameof(ReadOnlySpan<byte>.Slice), [typeof(int), typeof(int)])!;

    // Emits: BinaryPrimitives.ReadInt64LittleEndian(b.Slice(i + d, 8))
    private static void EmitLoadPacked8(ILGenerator il, int d)
    {
        il.Emit(OpCodes.Ldarg_0);                          // b  (ReadOnlySpan<byte>)
        il.Emit(OpCodes.Ldarg_1);                          // i
        if (d != 0)
        {
            il.Emit(OpCodes.Ldc_I4, d);
            il.Emit(OpCodes.Add);
        }
        il.Emit(OpCodes.Ldc_I4_8);                         // length = 8
        il.Emit(OpCodes.Call, SpanSlice);                  // b.Slice(i+d, 8)
        il.Emit(OpCodes.Call, ReadInt64LE);                // long
    }

    private static readonly MethodInfo ReadInt32LE =
        typeof(BinaryPrimitives).GetMethod(nameof(BinaryPrimitives.ReadInt32LittleEndian), [typeof(ReadOnlySpan<byte>)])!;

    // Emits: BinaryPrimitives.ReadInt32LittleEndian(b.Slice(i + d, 4))
    private static void EmitLoadPacked4(ILGenerator il, int d)
    {
        il.Emit(OpCodes.Ldarg_0);         // b
        il.Emit(OpCodes.Ldarg_1);         // i
        if (d != 0)
        {
            il.Emit(OpCodes.Ldc_I4, d);
            il.Emit(OpCodes.Add);
        }
        il.Emit(OpCodes.Ldc_I4_4);
        il.Emit(OpCodes.Call, SpanSlice);   // b.Slice(i+d, 4)
        il.Emit(OpCodes.Call, ReadInt32LE);
    }

    // Emits: a[i+d] | a[i+d+1]<<8 | a[i+d+2]<<16
    private static void EmitLoadPacked3(ILGenerator il, int d)
    {
        EmitLoadByte(il, d);
        EmitLoadByte(il, d + 1);
        il.Emit(OpCodes.Ldc_I4_8);
        il.Emit(OpCodes.Shl);
        il.Emit(OpCodes.Or);

        EmitLoadByte(il, d + 2);
        il.Emit(OpCodes.Ldc_I4, 16);
        il.Emit(OpCodes.Shl);
        il.Emit(OpCodes.Or);
    }
}
