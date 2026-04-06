using EToolkit.Domain;

namespace EToolkit.Application;

public interface IRecordImportService
{
    IReadOnlyList<ComponentPlacement> Import(Stream csvStream);
}