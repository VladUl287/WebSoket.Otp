using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using WebSockets.Otp.Core.Utils;

namespace WebSockets.Otp.Benchmark;

[MemoryDiagnoser]
public class TrieResolveSpeed
{
    private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("/api/users/{id}/sessions");

    public static readonly string[] ControllerPaths = new[]
    {
        "/api/users",
        "/api/users/{id}",
        "/api/users/{id}/profile",
        "/api/users/{id}/avatar",
        "/api/users/{id}/settings",
        "/api/users/{id}/roles",
        "/api/users/{id}/permissions",
        "/api/users/{id}/sessions",
        "/api/users/{id}/activity",
        "/api/users/me",
        "/api/users/search",
        "/api/users/export",
        "/api/users/import",
        "/api/users/bulk",
        "/api/users/activate",
        "/api/users/deactivate",

        "/api/auth/login",
        "/api/auth/logout",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/verify-email",
        "/api/auth/confirm-email",
        "/api/auth/change-password",
        "/api/auth/two-factor/enable",
        "/api/auth/two-factor/disable",
        "/api/auth/two-factor/verify",
        "/api/auth/sso/callback",
        "/api/auth/oauth/google",
        "/api/auth/oauth/github",
        "/api/auth/oauth/microsoft",

        "/api/products",
        "/api/products/{id}",
        "/api/products/{id}/images",
        "/api/products/{id}/reviews",
        "/api/products/{id}/variants",
        "/api/products/{id}/inventory",
        "/api/products/{id}/related",
        "/api/products/{id}/recommendations",
        "/api/products/search",
        "/api/products/filter",
        "/api/products/categories",
        "/api/products/brands",
        "/api/products/featured",
        "/api/products/new-arrivals",
        "/api/products/on-sale",
        "/api/products/bulk-update",

        "/api/orders",
        "/api/orders/{id}",
        "/api/orders/{id}/items",
        "/api/orders/{id}/status",
        "/api/orders/{id}/history",
        "/api/orders/{id}/invoice",
        "/api/orders/{id}/shipping",
        "/api/orders/{id}/tracking",
        "/api/orders/{id}/cancel",
        "/api/orders/{id}/refund",
        "/api/orders/{id}/return",
        "/api/orders/my-orders",
        "/api/orders/pending",
        "/api/orders/completed",
        "/api/orders/export",

        "/api/customers",
        "/api/customers/{id}",
        "/api/customers/{id}/orders",
        "/api/customers/{id}/addresses",
        "/api/customers/{id}/payment-methods",
        "/api/customers/{id}/wishlist",
        "/api/customers/{id}/cart",
        "/api/customers/{id}/loyalty",
        "/api/customers/search",
        "/api/customers/segments",
        "/api/customers/analytics"
    };

    private readonly static MetadataReference[] references =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
    ];

    private static readonly Assembly Assem = CSharpCompiler.Compile(
        CSharpTrieCodeGenerator.GenerateTrie([.. ControllerPaths.Select(Encoding.UTF8.GetBytes)]), references);

    public delegate int DayNameFn(byte[] bytes, int index);

    private static readonly MethodInfo Method = Assem.GetType("GeneratedTrie").GetMethod("Resolve");
    private static readonly DayNameFn Func = Assem.GetType("GeneratedTrie").GetMethod("Resolve").CreateDelegate<DayNameFn>();
    private static readonly IntPtr RawPointer = Method.MethodHandle.GetFunctionPointer();

    public static unsafe int Execute(byte[] bytes, int index)
    {
        // Cast the raw memory address directly to a managed static function pointer
        delegate* managed<byte[], int, int> nativeCall = (delegate* managed<byte[], int, int>)RawPointer;

        // This executes as a raw assembly indirect call instruction, bypassing the 14.8 ns delegate tax
        return nativeCall(bytes, index);
    }

    [Benchmark]
    public int Resolve_Imrpofew()
    {
        return Execute(Bytes, 0);
    }

    [Benchmark]
    public int Resolve_Reflection() => Func(Bytes, 0);

    [Benchmark]
    public int Resolve() => GeneratedTrie.Resolve(Bytes, 0);
}
