using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Rebus.Tests.Assumptions;

[TestFixture]
public class TestReflectInheritance
{
    [Test]
    public void Yes()
    {
        Print(typeof(BaseClass));
        Print(typeof(Overrider));
        Print(typeof(NonOverrider));

        var overridden = typeof(Overrider).GetMethod("SomeMethod", BindingFlags.Instance|BindingFlags.NonPublic);
        var notOverridden = typeof(NonOverrider).GetMethod("SomeMethod", BindingFlags.Instance|BindingFlags.NonPublic);

        Console.WriteLine();
    }

    void Print(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Instance|BindingFlags.NonPublic);

        Console.WriteLine("--------------------------------------------");
        Console.WriteLine(type.FullName);
        Console.WriteLine();
        Console.WriteLine(string.Join(Environment.NewLine, methods.Select(m => FormatMethod(m))));
        Console.WriteLine("--------------------------------------------");
        Console.WriteLine();
    }

    string FormatMethod(MethodInfo methodInfo)
    {
        return $"{methodInfo.Name}: {methodInfo.IsVirtual}";
    }

    abstract class BaseClass
    {
        protected virtual void SomeMethod()
        {
            Console.WriteLine("Hello there");
        }
    }

    class Overrider : BaseClass
    {
        protected override void SomeMethod()
        {
            Console.WriteLine("Hello from the overrider");
        }
    }

    class NonOverrider : BaseClass
    {
        // no overriding anything, no baby
    }
}