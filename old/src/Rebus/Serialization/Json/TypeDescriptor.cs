using System;

namespace Rebus.Serialization.Json
{
    /// <summary>
    /// Description of a .NET type that includes the name of an assembly
    /// and a fully qualified type name
    /// </summary>
    public class TypeDescriptor
    {
        /// <summary>
        /// Constructs the type desciptor
        /// </summary>
        public TypeDescriptor(string assemblyName, string typeName)
        {
            if (assemblyName == null) throw new ArgumentNullException("assemblyName");
            if (typeName == null) throw new ArgumentNullException("typeName");

            AssemblyName = assemblyName;
            TypeName = typeName;
        }

        /// <summary>
        /// Gets the assembly name
        /// </summary>
        public string AssemblyName { get; private set; }
        
        /// <summary>
        /// Gets the fully qualified type name
        /// </summary>
        public string TypeName { get; private set; }
    }
}