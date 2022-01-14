namespace Rebus.Pipeline;

/// <summary>
/// Indicates in which way a position is related to another step
/// </summary>
public enum PipelineRelativePosition
{
    /// <summary>
    /// Indicates that the step must be positioned before the other step
    /// </summary>
    Before, 
        
    /// <summary>
    /// Indicates that the step must be positioned after the other step
    /// </summary>
    After
}