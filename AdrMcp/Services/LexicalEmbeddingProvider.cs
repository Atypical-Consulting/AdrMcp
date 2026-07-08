using System.Text.RegularExpressions;
using AdrMcp.Interfaces;

namespace AdrMcp.Services;

/// <summary>
/// Zero-dependency term-frequency "embedding": tokenizes text into a normalized bag of
/// words and scores similarity via cosine. No external service or API key required.
/// A real embedding backend can replace this behind <see cref="IEmbeddingProvider"/>.
/// </summary>
public sealed class LexicalEmbeddingProvider : IEmbeddingProvider
{
    private static readonly Regex Token = new(@"[a-z0-9]+", RegexOptions.Compiled);

    private static readonly HashSet<string> Stop = new(StringComparer.Ordinal)
    {
        "the","a","an","and","or","but","if","then","of","to","in","on","for","with","as",
        "is","are","was","were","be","been","being","this","that","these","those","it","its",
        "we","our","you","your","they","their","i","by","at","from","which","will","shall",
        "can","could","should","would","may","might","must","not","no","do","does","did",
        "have","has","had","so","than","too","very","s","t"
    };

    public IReadOnlyDictionary<string, double> Embed(string text)
    {
        var counts = new Dictionary<string, double>();
        foreach (Match m in Token.Matches(text.ToLowerInvariant()))
        {
            var w = m.Value;
            if (w.Length < 2 || Stop.Contains(w)) continue;
            counts[w] = counts.TryGetValue(w, out var c) ? c + 1 : 1;
        }

        // L2-normalize so cosine == dot product.
        var norm = Math.Sqrt(counts.Values.Sum(v => v * v));
        if (norm == 0) return counts;
        foreach (var k in counts.Keys.ToList())
            counts[k] /= norm;
        return counts;
    }

    public double Similarity(IReadOnlyDictionary<string, double> a, IReadOnlyDictionary<string, double> b)
    {
        // Iterate the smaller vector for the dot product.
        var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
        double dot = 0;
        foreach (var (k, v) in small)
            if (large.TryGetValue(k, out var w))
                dot += v * w;
        return dot;
    }
}
