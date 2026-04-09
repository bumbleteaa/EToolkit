using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using EToolkit.Application;
using EToolkit.Infrastructure;

namespace EToolkit.Infrastructure;

public sealed class CsvRecordExporter : IRecordWriterService<CsvComponentPlacementRow>
{
    public async Task WriteAsync(IEnumerable<CsvComponentPlacementRow> rows, Stream output, CancellationToken cancellation)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(false), bufferSize: 64 * 1024, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            HasHeaderRecord = true,
            NewLine = Environment.NewLine // Quote all fields to be safe
        };

        await using var csv = new CsvWriter(writer, config);

        csv.Context.RegisterClassMap<CsvRowMap>();

        foreach (var row in rows)
        {
            cancellation.ThrowIfCancellationRequested();
            csv.WriteRecord(row);
            csv.NextRecord();
        }

        await writer.FlushAsync().WaitAsync(cancellation);
    }
}