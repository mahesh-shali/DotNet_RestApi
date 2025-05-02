public static class TokenBlacklist
{
    private static readonly HashSet<string> _blacklistedTokens = new HashSet<string>();

    public static void Add(string token)
    {
        _blacklistedTokens.Add(token);
    }

    public static bool IsBlacklisted(string token)
    {
        return _blacklistedTokens.Contains(token);
    }
}
