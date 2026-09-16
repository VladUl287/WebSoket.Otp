using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using WebSockets.Otp.Abstractions.Utils;
using WebSockets.Otp.Core.Models;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Core.Services.Endpoints;

public unsafe sealed class EndpointResolver : ITrieResolver<WsEndpointInfo>
{
    private readonly WsEndpointInfo[] _types;
    private readonly Func<byte[], int, int> _resolve;

    public EndpointResolver(byte[][] values, WsEndpointInfo[] types)
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

        var code = CSharpTrieCodeGenerator.GenerateTrie(values);
        var assembly = CSharpCompiler.Compile(code, references);
        var type = assembly.GetType("GeneratedTrie") ?? throw new NullReferenceException();
        var method = type.GetMethod("Resolve") ?? throw new NullReferenceException();

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
    }
}
