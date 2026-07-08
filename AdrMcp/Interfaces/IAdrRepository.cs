using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>Reads and writes ADR markdown files under the configured ADR root.</summary>
public interface IAdrRepository
{
    /// <summary>Absolute path of the ADR root directory (e.g. <c>.../docs/adr</c>).</summary>
    string AdrRoot { get; }

    /// <summary>Loads and parses every ADR in the root (index/README files excluded).</summary>
    IReadOnlyList<Adr> LoadAll();

    /// <summary>Finds a single ADR by numeric id, zero-padded id, or slug. Null if absent.</summary>
    Adr? Find(string idOrSlug);

    /// <summary>The next available sequential id (max existing + 1, or 1 when empty).</summary>
    int NextId();

    /// <summary>Renders an ADR to its full on-disk markdown (frontmatter + body).</summary>
    string Render(Adr adr);

    /// <summary>Resolves the absolute path an ADR would occupy on disk.</summary>
    string PathFor(Adr adr);

    /// <summary>Persists an ADR to disk (creating the root directory if needed).</summary>
    void Save(Adr adr);

    /// <summary>Reads the current on-disk content of an ADR file, or empty if it does not exist.</summary>
    string ReadRaw(Adr adr);
}
