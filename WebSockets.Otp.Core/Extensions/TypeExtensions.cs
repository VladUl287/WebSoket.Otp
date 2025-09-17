namespace WebSockets.Otp.Core.Extensions;

public static class TypeExtensions
{
    public static bool ImplementsInterface<TInterface>(this Type type) => type is not null && typeof(TInterface).IsAssignableFrom(type);
}
