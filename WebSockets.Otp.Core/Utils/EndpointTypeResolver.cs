using Microsoft.CodeAnalysis;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Utils;

public unsafe sealed class EndpointTypeResolver : ITrieResolver
{
    private readonly Type[] _types;
    private readonly Func<byte[], int, int> _resolve;

    public EndpointTypeResolver(byte[][] values, Type[] types)
    {
        _types = types;

        //var references = AppDomain.CurrentDomain.GetAssemblies()
        //    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        //    .Select(a => MetadataReference.CreateFromFile(a.Location))
        //    .Cast<MetadataReference>()
        //    .ToList();

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var trie = CSharpTrieCodeGenerator.GenerateTrie(values);

        var cmod = CSharpCompiler.Compile(trie, references);
        var type = cmod.GetType("GeneratedTrie");
        var method = type.GetMethod("Resolve");

        _resolve = method.CreateDelegate<Func<byte[], int, int>>();
    }

    public Type Resolve(byte[] sequence)
    {
        return Resolve(sequence.AsSpan());
    }

    public Type Resolve(ReadOnlySpan<byte> sequence)
    {
        var index = _resolve(sequence.ToArray(), 0);

        if(index == -1)
            throw new Exception();

        return _types[index];
        //return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_types), (nint)index);
    }
}
