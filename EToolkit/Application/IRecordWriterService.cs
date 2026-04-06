namespace EToolkit.Application;

public interface IRecordWriterService<in T>
{
    Task WriteAsync(IEnumerable<T> rows, Stream output, CancellationToken cancellation);
}