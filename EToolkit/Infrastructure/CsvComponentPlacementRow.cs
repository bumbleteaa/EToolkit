namespace EToolkit.Infrastructure;
// TODO Non nullable reference type for CsvComponentPlacementRow, we can use record struct if we want to make it immutable, or we can use class with init property if we want to make it mutable. For now, we will use class with init property for simplicity, and we can change it to record struct later if needed.
public class CsvComponentPlacementRow
{
    public string comp { get; set; }
    public string FeederId { get; set; }
    public string Nozzle { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public string Footprint { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Rotation { get; set; }
    public string Desc { get; set; }
    public string Side { get; set; }
}