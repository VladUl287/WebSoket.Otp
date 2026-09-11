using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace WebSockets.Otp.CodeGen
{
    [Generator]
    public class TrieCodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                //var trie = CSharpTrieCodeGenerator.GenerateTrie((fieldsBytes));

                //ctx.AddSource("TrieResolver.g.cs", SourceText.From(trie, Encoding.UTF8));
            });
        }
    }
}