using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>
/// Resolves whether an ADR's <see cref="CodeRef"/> still points at live code.
/// The default provider is filesystem/text based and language-agnostic; a Roslyn
/// (.NET) or codebase-memory provider can be plugged in behind this interface.
/// </summary>
public interface ICodeLinkProvider
{
    /// <summary>Name of the provider (for diagnostics / reporting).</summary>
    string Name { get; }

    /// <summary>True if the reference resolves against the repository at <paramref name="repoRoot"/>.</summary>
    bool Resolves(CodeRef reference, string repoRoot);
}
