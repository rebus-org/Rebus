using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Rebus.Tests.Synchronous;

[TestFixture]
public class TestSyncBusApiParity
{
    [TestCase(typeof(IBus), typeof(ISyncBus))]
    public void CompareApis(Type originalApi, Type synchronousVersion)
    {
        var originalOperations = originalApi.GetMethods().Where(IsAsyncMethod);

        var missingOnSynchronousApi = originalOperations
            .Select(originalMethod => new
            {
                Method = originalMethod,
                ErrorReason = IsReplicated(originalMethod, synchronousVersion)
            })
            .Where(a => a.ErrorReason != null)
            .ToList();

        if (!missingOnSynchronousApi.Any()) return;

        throw new AssertionException($@"The original async {originalApi} API compared to the sync {synchronousVersion} API is missing the following operations:

{string.Join(Environment.NewLine, missingOnSynchronousApi.Select(m => $"    {FormatMethodSignature(m.Method)} - {m.ErrorReason}"))}

");
    }

    static bool IsAsyncMethod(MethodInfo method)
    {
        var returnType = method.ReturnType;

        if (!returnType.GetTypeInfo().IsGenericType)
            return returnType == typeof(Task);

        return returnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    static string FormatMethodSignature(MethodInfo methodInfo)
    {
        return $"{FormatType(methodInfo.ReturnType)} {methodInfo.Name}({string.Join(", ", methodInfo.GetParameters().Select(p => $"{FormatType(p.ParameterType)} {p.Name}"))})";
    }

    static string FormatType(Type type)
    {
        if (!type.GetTypeInfo().IsGenericType) return type.Name;

        var typeParameters = type.GetGenericArguments();

        var justTheName = type.Name.Substring(0, type.Name.IndexOf("`"));

        return $"{justTheName}<{string.Join(", ", typeParameters.Select(FormatType))}>";
    }

    static string IsReplicated(MethodInfo originalMethod, Type synchronousVersion)
    {
        var name = originalMethod.Name;

        var synchronousMethod = synchronousVersion.GetMethod(name, originalMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        if (synchronousMethod == null) return "Could not find method";

        var originalMethodParameters = originalMethod.GetParameters();
        var replicatedMethodParameters = synchronousMethod.GetParameters();

        if (originalMethodParameters.Length != replicatedMethodParameters.Length)
            return "Methods do not accept the same parameters";

        foreach (var parameterPair in originalMethodParameters.Zip(replicatedMethodParameters, (p1, p2) => new { Original = p1, Replicated = p2 }))
        {
            if (parameterPair.Original.Name != parameterPair.Replicated.Name) return "Parameter names do not match";
            if (parameterPair.Original.ParameterType != parameterPair.Replicated.ParameterType) return "Parameter types do not match";
        }

        return null;
    }
}