namespace EToolkit.Domain;

public record ComponentPlacement
{
        Component Component { get;}
        string FeederId { get;}
        string Nozzle {get;}
        Side Side { get;}
        Position Position { get;}
        double Rotation { get;}

        public ComponentPlacement(Component component, 
            string feederId, 
            string nozzle, 
            Side side,
            Position position,
            double rotation)
        {
            Component = component;
            FeederId = feederId;
            Nozzle = nozzle;
            Side = side;
            Position = position;
            Rotation = rotation;
        }
}