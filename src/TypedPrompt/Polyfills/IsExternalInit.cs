#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>Lets records and <c>init</c> setters compile for netstandard2.0, which lacks this marker type.</summary>
internal static class IsExternalInit
{
}
#endif
