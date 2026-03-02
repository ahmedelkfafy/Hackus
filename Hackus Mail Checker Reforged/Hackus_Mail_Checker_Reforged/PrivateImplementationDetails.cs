// Auto-generated stub for compiler-generated class used in switch-on-string optimizations
internal static class _PrivateImplementationDetails_
{
    internal static uint ComputeStringHash(string s)
    {
        uint hashCode = 2166136261u;
        if (s != null)
        {
            for (int i = 0; i < s.Length; i++)
            {
                hashCode = (hashCode ^ s[i]) * 16777619u;
            }
        }
        return hashCode;
    }
}
