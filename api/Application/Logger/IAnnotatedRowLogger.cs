namespace EToolkit.Application.Logger;

/// <summary>
/// Defines the contract for writing a log entry from a classified row.
/// Responsible for HOW a log entry is emitted — format, level, and fields.
/// Has no knowledge of dedup, policy, or when logging should occur.
/// </summary>
public interface IAnnotatedRowLogger
{
    // Emits a single log entry for the given annotated row
    void Log(AnnotatedRow annotated);
}