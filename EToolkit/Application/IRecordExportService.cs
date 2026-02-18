namespace EToolkit.Application;

public interface IRecordExportService<in T>
{
    Task WriteAsync(IEnumerable<T> rows, Stream output, CancellationToken cancellation);
}