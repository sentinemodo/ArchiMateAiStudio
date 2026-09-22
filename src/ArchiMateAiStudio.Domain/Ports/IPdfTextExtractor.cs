namespace ArchiMateAiStudio.Domain.Ports;

public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts the text layer from a PDF document.
    /// </summary>
    string ExtractText(ReadOnlySpan<byte> pdfBytes);
}
