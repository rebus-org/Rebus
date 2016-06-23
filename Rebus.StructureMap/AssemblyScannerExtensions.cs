using StructureMap.Graph;

namespace Rebus.StructureMap
{
    public static class AssemblyScannerExtensions
    {
        public static void WithMessageHanderConvetion(this IAssemblyScanner scanner)
        {
            scanner.With(new MessageHandlerConvention());
        }
    }
}