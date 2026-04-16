using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordImportService
{
    AnnotatedRow[] Import(Stream csvStream);
}