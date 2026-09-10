using System.Reflection;
using System.Reflection.Emit;
using WebSockets.Otp.Abstractions.Utils;

namespace WebSockets.Otp.Core.Utils;

public sealed class TrieResolver<T> : ITrieResolver<T>
{
    public TrieResolver(byte[][] values)
    {
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType("DynamicCalculator", TypeAttributes.Public | TypeAttributes.Class);
        
        var methodBuilder = typeBuilder.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int), [typeof(int), typeof(int)]);
        
        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); // Load first argument
        il.Emit(OpCodes.Ldarg_1); // Load second argument
        il.Emit(OpCodes.Add); // Add them
        il.Emit(OpCodes.Ret); // Return result

        var dynamicType = typeBuilder.CreateType();
        var method = dynamicType.GetMethod("Add");

        var resolve = Delegate.CreateDelegate(typeof(Func<int, int, int>), method);
    }

    public T Resolve(byte[] sequence)
    {
        return Resolve(sequence.AsSpan());
    }

    public T Resolve(ReadOnlySpan<byte> sequence)
    {
        throw new NotImplementedException();
    }
}
