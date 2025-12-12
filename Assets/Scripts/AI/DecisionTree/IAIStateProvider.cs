namespace Ashore.AI.DecisionTree
{
    /// <summary>
    /// Interface for enemies that expose their AI state for telemetry logging.
    /// Implement this on enemy classes that need to log their AI decisions.
    /// </summary>
    public interface IAIStateProvider
    {
        /// <summary>
        /// Gets the current AI state for telemetry purposes.
        /// </summary>
        Utilities.SimpleEnemyState CurrentAIState { get; }
    }
}
