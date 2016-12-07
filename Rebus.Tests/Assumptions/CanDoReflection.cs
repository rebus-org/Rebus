//using System;
//using System.Reflection;
//using Microsoft.Extensions.DependencyModel;
//using Rebus.Bus;
//using Rebus.Handlers;
//
//namespace Rebus.Tests.Assumptions
//{
//    public class CanDoReflection
//    {
//        public void YeahItWorks()
//        {
//            IBus bus = GetBus();
//
//            var assemblyToScan = typeof(CanDoReflection).GetTypeInfo().Assembly;
//
//            var handledMessageTypes = assemblyToScan
//                .GetTypes()
//                .SelectMany(t => t.GetInterfaces()
//                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IHandleMessages<>))
//                    .Select(i => i.GetGenericArguments().Single()))
//                .Distinct()
//                .ToList();
//
//            foreach (var messageType in handledMessageTypes)
//            {
//                bus.Advanced.Topics.Subscribe(messageType.GetSimpleAssemblyQualifiedName());
//            }
//        }
//
//        IBus GetBus()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}