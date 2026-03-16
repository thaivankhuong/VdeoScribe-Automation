namespace Whiteboard.Export.Models;

public record ExportResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public int ExportedFrameCount { get; init; }
    public int TotalOperations { get; init; }
    public string DeterministicKey { get; init; } = string.Empty;
}
