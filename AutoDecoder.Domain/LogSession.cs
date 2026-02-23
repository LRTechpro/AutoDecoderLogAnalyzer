using AutoDecoder.Models;
using AutoDecoder.Protocols.Utilities;

namespace AutoDecoder.Domain;

// Represents one loaded log + decoded results + summary.
// Encapsulation: setters are private; updates happen through methods.
public sealed class LogSession
{
    // Unique ID for this session.
    public Guid SessionId { get; } = Guid.NewGuid();

    // Friendly name shown in the UI (file name or user label).
    public string Name { get; private set; }

    // Full file path (optional, but useful for reload / provenance).
    public string? FilePath { get; private set; }

    // When the session was created.
    public DateTime CreatedAt { get; } = DateTime.Now;

    // Encapsulated list of decoded lines. Expose read-only to the UI.
    private readonly List<LogLine> _lines = new();
    public IReadOnlyList<LogLine> Lines => _lines;

    // Cached summary for this session.
    public FindingsSummary? Summary { get; private set; }

    // Optional: simple risk score computed from the summary (encapsulation).
    public int RiskScore { get; private set; }

    // Constructor for file-backed session.
    public LogSession(string name, string filePath)
    {
        Name = name;
        FilePath = filePath;
    }

    // Constructor for non-file session (e.g., pasted text / sample).
    public LogSession(string name)
    {
        Name = name;
    }

    // Replace decoded lines (single controlled entry point).
    public void SetDecodedLines(IEnumerable<LogLine> decodedLines)
    {
        // Clear existing lines.
        _lines.Clear();

        // Add new decoded lines.
        _lines.AddRange(decodedLines);

        // Recompute summary and score every time lines change.
        RebuildSummary();
    }

    // Rebuild summary and risk score from current lines.
    public void RebuildSummary()
    {
        // Build deterministic summary from decoded lines.
        Summary = FindingsAggregator.Build(_lines);

        // Compute a simple risk score from summary (tune later).
        RiskScore =
            (Summary?.UdsFindingLines ?? 0) * 2 +
            (Summary?.UnknownLines ?? 0) * 1 +
            (Summary?.NrcCounts?.Values.Sum() ?? 0) * 3;
    }

    // Rename session safely.
    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        Name = newName.Trim();
    }
}