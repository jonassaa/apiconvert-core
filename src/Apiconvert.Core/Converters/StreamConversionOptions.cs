using System.Text;

namespace Apiconvert.Core.Converters;

/// <summary>
/// Supported stream input shapes for item-by-item conversion.
/// </summary>
public enum StreamInputKind
{
    /// <summary>
    /// JSON payload with a top-level array of items.
    /// </summary>
    JsonArray,
    /// <summary>
    /// Newline-delimited JSON (one JSON item per non-empty line).
    /// </summary>
    Ndjson,
    /// <summary>
    /// Newline-delimited query strings (one record per non-empty line).
    /// </summary>
    QueryLines,
    /// <summary>
    /// XML payload where each matched element path is treated as one item.
    /// </summary>
    XmlElements
}

/// <summary>
/// Error handling mode for stream item failures.
/// </summary>
public enum StreamErrorMode
{
    /// <summary>
    /// Throw immediately on the first parse or conversion error.
    /// </summary>
    FailFast,
    /// <summary>
    /// Continue yielding results and include item-level errors in each result.
    /// </summary>
    ContinueWithReport
}

/// <summary>
/// Options controlling stream item extraction and error behavior.
/// </summary>
public sealed record StreamConversionOptions
{
    /// <summary>
    /// Input shape adapter. Defaults to <see cref="StreamInputKind.JsonArray"/>.
    /// </summary>
    public StreamInputKind InputKind { get; init; } = StreamInputKind.JsonArray;

    /// <summary>
    /// Error handling mode for per-item parse/conversion failures.
    /// Defaults to <see cref="StreamErrorMode.FailFast"/>.
    /// </summary>
    public StreamErrorMode ErrorMode { get; init; } = StreamErrorMode.FailFast;

    /// <summary>
    /// Text encoding used by line-based adapters.
    /// Defaults to UTF-8.
    /// </summary>
    public Encoding? Encoding { get; init; }

    /// <summary>
    /// Dot-separated XML element path used when <see cref="InputKind"/> is
    /// <see cref="StreamInputKind.XmlElements"/> (for example: "orders.order").
    /// </summary>
    public string? XmlItemPath { get; init; }
}
