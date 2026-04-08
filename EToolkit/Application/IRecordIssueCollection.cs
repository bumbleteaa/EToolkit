using EToolkit.Infrastructure;

namespace EToolkit.Application;

public interface IRecordIssueCollector
{
    void Report(AnnotatedRow annotated);
}