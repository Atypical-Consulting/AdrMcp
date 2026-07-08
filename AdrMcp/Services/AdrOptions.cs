namespace AdrMcp.Services;

/// <summary>
/// Runtime configuration. <see cref="Root"/> is where ADR files live; <see cref="RepoRoot"/>
/// is the base used to resolve <c>code_refs</c> when checking for stale decisions.
/// </summary>
public sealed record AdrOptions(string Root, string RepoRoot)
{
    /// <summary>
    /// Resolves options from (in order) an explicit <c>--adr-root</c> arg, the
    /// <c>ADR_ROOT</c> env var, then <c>./docs/adr</c> under the current directory.
    /// </summary>
    public static AdrOptions Resolve(string[] args)
    {
        string? root = ArgValue(args, "--adr-root")
                       ?? Environment.GetEnvironmentVariable("ADR_ROOT");

        string repoRoot = ArgValue(args, "--repo-root")
                          ?? Environment.GetEnvironmentVariable("ADR_REPO_ROOT")
                          ?? Directory.GetCurrentDirectory();

        root ??= Path.Combine(repoRoot, "docs", "adr");

        return new AdrOptions(Path.GetFullPath(root), Path.GetFullPath(repoRoot));
    }

    private static string? ArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];

        // also support --adr-root=value
        string prefix = name + "=";
        return args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];
    }
}
