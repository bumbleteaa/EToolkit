using CsvHelper;
using System.Globalization;
using EToolkit.Domain;
using EToolkit.Infrastructure;

namespace EToolkit.Application;

// Importing service that reads a CSV stream and converts it to a list of ComponentPlacement records.
public class RecordImportService : IRecordImportService
{
    public IReadOnlyList<ComponentPlacement> Import(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvRowMap>();

        var rows = csv.GetRecords<CsvComponentPlacementRow>();
        var result = new List<ComponentPlacement>();

        // Convert each CSV row to a ComponentPlacement domain model using the mapping defined in CsvFileMapping.
        foreach (var row in rows)
        {
            result.Add(CsvFileMapping.ToDomain(row));
        }
        return result;
    }
}