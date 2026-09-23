using ArchiMateAiStudio.Domain.Ports;
using UglyToad.PdfPig;

namespace ArchiMateAiStudio.Infrastructure.Ingestion;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public string ExtractText(ReadOnlySpan<byte> pdfBytes)
    {
        if (pdfBytes.IsEmpty)
        {
            throw new ArgumentException("PDF bytes are empty.", nameof(pdfBytes));
        }

        using var stream = new MemoryStream(pdfBytes.ToArray());
        using var document = PdfDocument.Open(stream);
        var parts = new List<string>();
        foreach (var page in document.GetPages())
        {
            var pageText = page.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(pageText))
            {
                parts.Add(pageText);
            }
        }

        return string.Join("\n", parts).Trim();
    }
}
