namespace EToolkit.Infrastructure;

public interface ICsvRecordImporter
{
    IEnumerable<CsvComponentPlacementRow> Import(Stream csvStream);
}