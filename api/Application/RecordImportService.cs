using EToolkit.Infrastructure;

namespace EToolkit.Application;

// Use case for importing records from a CSV stream, applying filtering, and returning the filtered list of CsvComponentPlacementRow.
public class RecordImportService : IRecordImportService
{
    private readonly ICsvRecordImporter _importer;
    private readonly ILogger<RecordImportService> _logger;

    public RecordImportService(ICsvRecordImporter importer, ILogger<RecordImportService> logger)
    {
        _importer = importer;
        _logger = logger;
    }

    public CsvComponentPlacementRow[] Import(Stream csvStream)
    {
        // Count raw lines before parsing to detect any rows lost during parse.
        // Stream must support seeking. Reset to position 0 after counting.
        var totalParsed = CountLines(csvStream) - 1;
        csvStream.Seek(0, SeekOrigin.Begin);

        var rows = _importer.Import(csvStream).ToArray();
        // TP-1: baseline count — all downstream testpoints should account for this total
        _logger.LogInformation("[TP-1] Import complete: {Parsed} / {Total} rows parsed from CSV", rows.Length, totalParsed);

        return rows;
    }
    // Counts newline-delimited lines without loading the entire stream into memory.
    private static int CountLines(Stream stream)
    {
        var count = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (reader.ReadLine() is not null) count++;
        return count;
    }
}



