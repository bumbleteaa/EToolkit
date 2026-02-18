using EToolkit.Application;
using EToolkit.Domain;
using EToolkit.Infrastructure;

namespace EToolkit.Infrastructure;

// This class is responsible for mapping a CSV row to a domain object, which can be used for further processing in the application. It also provides a method to parse the side of the component, which can be used for filtering and grouping components.
public class CsvFileMapping
{
    // ComponentPlacement ToDomain method takes a CsvComponentPlacementRow and converts it to a ComponentPlacement domain object. 
    public static ComponentPlacement ToDomain(CsvComponentPlacementRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Name))
            throw new FormatException("Name is required.");

        if (string.IsNullOrWhiteSpace(row.Value))
            throw new FormatException("Value is required.");

        if (string.IsNullOrWhiteSpace(row.Footprint))
            throw new FormatException("Footprint is required.");

        if (string.IsNullOrWhiteSpace(row.Side))
            throw new FormatException("Side is required.");

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