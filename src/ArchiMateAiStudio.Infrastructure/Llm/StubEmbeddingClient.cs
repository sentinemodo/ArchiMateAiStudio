using System.Text;
using ArchiMateAiStudio.Domain.Ports;

namespace ArchiMateAiStudio.Infrastructure.Llm;

/// <summary>
/// Deterministic bag-of-words hash embedding for CI / in-memory RAG. Not a real model.
/// </summary>
public sealed class StubEmbeddingClient : IEmbeddingClient
{
    public const int DefaultDimensions = 64;

    public int Dimensions { get; }

    public StubEmbeddingClient(int dimensions = DefaultDimensions)
    {
        if (dimensions < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be at least 8.");
        }

        Dimensions = dimensions;
    }

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Embed(text ?? string.Empty));
    }

    public float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        if (string.IsNullOrWhiteSpace(text))
        {
            return vector;
        }

        var tokens = Tokenize(text);
        foreach (var token in tokens)
        {
            var hash = StableHash(token);
            var index = (int)(hash % (uint)Dimensions);
            var sign = (hash & 1) == 0 ? 1f : -1f;
            vector[index] += sign;
        }

        Normalize(vector);
        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var sb = new StringBuilder();
        foreach (var ch in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            yield return sb.ToString();
        }
    }

    private static uint StableHash(string token)
    {
        // FNV-1a 32-bit
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in token)
            {
                hash ^= ch;
                hash *= 16777619;
            }

            return hash;
        }
    }

    private static void Normalize(float[] vector)
    {
        double sumSquares = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        if (sumSquares <= 0)
        {
            return;
        }

        var norm = (float)Math.Sqrt(sumSquares);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }
}
