using Xunit;

// Disabled parallel execution as it caused random failing tests. Need to look into this further though.

[assembly: CollectionBehavior(DisableTestParallelization = true)]