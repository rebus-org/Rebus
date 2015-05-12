namespace Rebus.Pipeline
{
    /// <summary>
    /// Models a step that is associated with a stage (i.e. some kind of indication of the step's desired position within its pipeline)
    /// </summary>
    /// <typeparam name="TStep"></typeparam>
    /// <typeparam name="TStage"></typeparam>
    public class StagedStep<TStep, TStage> where TStep : IStep
    {
        /// <summary>
        /// Constructs the staged step with the given step and stage
        /// </summary>
        public StagedStep(TStep step, TStage stage)
        {
            Step = step;
            Stage = stage;
        }

        /// <summary>
        /// This is the step held by this staged step
        /// </summary>
        public TStep Step { get; private set; }

        /// <summary>
        /// This is the desired pipeline stage held by this staged step
        /// </summary>
        public TStage Stage { get; private set; }
    }
}