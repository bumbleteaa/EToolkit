using System.Globalization;
using CsvHelper;

//Turn a CSV file into C# objects that look like the CSV
namespace EToolkit.Infrastructure;

public class CsvRecordImporter : ICsvRecordImporter
{
    public IEnumerable<CsvComponentPlacementRow> Import(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Context.RegisterClassMap<CsvRowMap>();
        
        return csv.GetRecords<CsvComponentPlacementRow>().ToList();
    }
}