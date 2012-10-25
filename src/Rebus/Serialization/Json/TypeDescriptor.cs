namespace Rebus.Serialization.Json
{
    public class TypeDescriptor
    {
        public TypeDescriptor(string assemblyName, string typeName)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
        }

        public string AssemblyName { get; private set; }
        public string TypeName { get; private set; }
    }
}