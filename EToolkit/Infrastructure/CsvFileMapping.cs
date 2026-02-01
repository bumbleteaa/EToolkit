using EToolkit.Application;
using EToolkit.Domain;
using EToolkit.Infrastructure;

namespace EToolkit.Infrastructure;

public class CsvFileMapping
{
    public static ComponentPlacement ToDomain(CsvComponentPlacementRow row)
    {
        return new ComponentPlacement(
            new Component(row.Name, row.Value, row.Footprint),
            row.FeederId, 
            row.Nozzle, 
            ParseSide(row.Side), 
            new Position(row.X, row.Y), row.Rotation
            );
    }

    private static Side ParseSide(string side)
    {
        return side switch
        {
            "Top" => Side.Top,
            "Bottom" => Side.Bottom,
            _ => throw new Exception($"Invalid side {side}")
        };
    }
}