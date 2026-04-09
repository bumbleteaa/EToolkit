using CsvHelper.Configuration;
using EToolkit.Domain;

namespace EToolkit.Infrastructure;

public sealed class CsvRowMap : ClassMap<CsvComponentPlacementRow>
{
    public CsvRowMap()
    {
        Map(m => m.comp).Name("comp");
        Map(m => m.FeederId).Name("Feeder ID");
        Map(m => m.Nozzle).Name("Nozzle");
        Map(m => m.Name).Name("Name");
        Map(m => m.Value).Name("Value");
        Map(m => m.Footprint).Name("Footprint");
        Map(m => m.X).Name("X");
        Map(m => m.Y).Name("Y");
        Map(m => m.Rotation).Name("Rotation");
        Map(m => m.Desc).Name("Desc");
        Map(m => m.Side).Name("Side");
    }
}