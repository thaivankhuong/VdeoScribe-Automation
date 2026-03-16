using Whiteboard.Export.Models;

namespace Whiteboard.Export.Contracts;

public interface IExportPipeline
{
    ExportResult Export(ExportRequest request);
}
