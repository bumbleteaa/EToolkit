using CsvHelper;
using System.Globalization;
using EToolkit.Domain;
using EToolkit.Infrastructure;

namespace EToolkit.Application;

//Bring placement data into the system
//converts to Placement (domain)
public class RecordImportService
{
    public IReadOnlyList<ComponentPlacement> Import(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<CsvRowMap>();

        var rows = csv.GetRecords<CsvComponentPlacementRow>();
        var result = new List<ComponentPlacement>();

        foreach (var row in rows)
        {
            result.Add(CsvFileMapping.ToDomain(row));
        }
        return result;
    }
}