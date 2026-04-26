namespace EToolkit.Application.Logger;

/// <summary>
/// Defines the contract for deciding whether a classified row warrants a log entry.
/// Responsible for WHY a log entry should occur — based on row status and business rules.
/// Has no knowledge of formatting, log levels, or ILogger infrastructure.
/// </summary>
public interface IAnnotatedRowLogPolicy
{
    // Returns true if the given annotated row should be logged
    bool ShouldLog(AnnotatedRow annotated);
}