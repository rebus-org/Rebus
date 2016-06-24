using StructureMap.Graph;

namespace Rebus.StructureMap
{
    /// <summary>
    /// StructureMap assembly scanner extensions for Rebus
    /// </summary>
    public static class AssemblyScannerExtensions
    {
        /// <summary>
        /// Uses <see cref="MessageHandlerConvention"/> on the scanner to auto-register found Rebus handlers
        /// </summary>
        public static void WithMessageHanderConvention(this IAssemblyScanner scanner)
        {
            scanner.With(new MessageHandlerConvention());
        }
    }
}