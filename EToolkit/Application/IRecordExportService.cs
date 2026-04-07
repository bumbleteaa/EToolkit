namespace EToolkit.Application;

public interface IRecordExportService
{
    Task ExportAsync(Stream csvInput, Stream csvOutput, int? take, IReadOnlySet<string> acceptedOverrides, CancellationToken cancellation);
}