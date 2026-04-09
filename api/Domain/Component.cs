namespace EToolkit.Domain;
// * This record represents a component in the PCB design, it contains the name, value, and footprint of the component. It is used as the input for the filtering and normalization process, and can be used for logging and debugging purposes.
public record Component(string Name, string Value, string Footprint);