using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;

namespace WebSockets.Otp.Core.Utils;

public unsafe sealed class EndpointTypeResolver : ITrieResolver<WsEndpointInfo>
{
    private readonly WsEndpointInfo[] _types;
    private readonly Func<byte[], int, int> _resolve;

    public EndpointTypeResolver(byte[][] values, WsEndpointInfo[] types)
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

    public bool TryResolve(ReadOnlySpan<byte> sequence, [NotNullWhen(true)] out WsEndpointInfo? value)
    {
        value = null;

        var index = _resolve(sequence.ToArray(), 0);

        if (index == -1)
            return false;

        value = _types[index];
        return true;
        //return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_types), (nint)index);
    }
}
