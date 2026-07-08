namespace AdrMcp.Interfaces;

/// <summary>
/// Turns text into a sparse term vector and scores similarity between vectors.
/// The default implementation is lexical (no external service); a real embedding
/// backend can be swapped in behind this interface.
/// </summary>
public interface IEmbeddingProvider
{
    IReadOnlyDictionary<string, double> Embed(string text);

    double Similarity(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b);
}
