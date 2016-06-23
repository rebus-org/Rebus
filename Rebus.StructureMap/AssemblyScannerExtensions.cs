using StructureMap.Graph;

namespace Rebus.StructureMap
{
    public static class AssemblyScannerExtensions
    {
        public static void WithMessageHanderConvention(this IAssemblyScanner scanner)
        {
            scanner.With(new MessageHandlerConvention());
        }
    }
}