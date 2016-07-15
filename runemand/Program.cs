using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Rebus.Activation;
using Rebus.Autofac;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Persistence.SqlServer;
using Rebus.Routing.TypeBased;
using Rebus.Sagas;
using Rebus.Transport.SqlServer;

namespace RebusMultipleSagaSameMessage
{
    internal class Program
    {
        private const string connectionstring = @"Server=.;Database=rebus2_test;Trusted_Connection=True;";

        private static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            IContainer container;


            builder.RegisterAssemblyTypes(typeof(StartCommand1).Assembly)
                .Where(x => typeof(IHandleMessages).IsAssignableFrom(x))
                .AsClosedTypesOf(typeof(IHandleMessages<>))
                .AsImplementedInterfaces()
                .InstancePerDependency()
                .PropertiesAutowired();

            container = builder.Build();


            var adapter = new AutofacContainerAdapter(container);
            IBus Bus = Configure.With(adapter)

                .Transport(x =>
                {
                    x.UseSqlServer(connectionstring, "rebustable", "rebbusqueue");

                }).Routing(r => r.TypeBased().MapAssemblyOf<StartCommand1>("rebbusqueue"))
                .Sagas(s => s.StoreInSqlServer(connectionstring, "RebusSagaStorage", "RebusSagaIndex"))
                .Start();



            Bus.SendLocal(new StartCommand1() { Id = Guid.NewGuid(), Prop1 = 1, Prop2 = true });
            Bus.SendLocal(new StartCommand2() { Id = Guid.NewGuid(), Prop1 = false, Prop2 = DateTime.UtcNow });

            Console.ReadLine();

        }


    }

    public class Saga1 : Saga<SagaData1>
        , IAmInitiatedBy<StartCommand1>
        , IHandleMessages<DoWorkCompleteCommand>
    {
        public IBus Bus { get; set; }
        protected override void CorrelateMessages(ICorrelationConfig<SagaData1> config)
        {
            config.Correlate<StartCommand1>(x => x.SagaId, y => y.Id);
            config.Correlate<DoWorkCompleteCommand>(x => x.SagaId, y => y.Id);
        }

        public async Task Handle(StartCommand1 message)
        {
            if (!IsNew) return;
            Data.Id = Guid.NewGuid();
            Data.SagaProp1 = "Message sent to do work";
            Data.Command = message;
            await Bus.SendLocal(new DoWorkCommand() { SagaId = Data.Id });
        }

        public async Task Handle(DoWorkCompleteCommand message)
        {
            MarkAsComplete();
        }
    }

    public class Saga2 : Saga<SagaData2>
       , IAmInitiatedBy<StartCommand2>
       , IHandleMessages<DoWorkCompleteCommand>
    {
        public IBus Bus { get; set; }
        protected override void CorrelateMessages(ICorrelationConfig<SagaData2> config)
        {
            config.Correlate<StartCommand2>(x => x.SagaId, y => y.Id);
            config.Correlate<DoWorkCompleteCommand>(x => x.SagaId, y => y.Id);
        }

        public async Task Handle(StartCommand2 message)
        {
            if (!IsNew) return;
            Data.Id = Guid.NewGuid();
            Data.SagaTimeStamp = DateTime.UtcNow;
            Data.Command = message;
            await Bus.SendLocal(new DoWorkCommand() { SagaId = Data.Id });
        }

        public async Task Handle(DoWorkCompleteCommand message)
        {
            MarkAsComplete();
        }
    }

    public class DoWorkCommandHandler : IHandleMessages<DoWorkCommand>
    {
        public IBus Bus { get; set; }
        public async Task Handle(DoWorkCommand message)
        {
            await Bus.SendLocal(new DoWorkCompleteCommand() { SagaId = message.SagaId });
        }
    }

    public class StartCommand2 : BaseSagaStartCommand
    {
        public Guid Id { get; set; }
        public bool Prop1 { get; set; }
        public DateTime Prop2 { get; set; }
    }

    public class SagaData2 : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }
        public DateTime SagaTimeStamp { get; set; }
        public StartCommand2 Command { get; set; }
    }

    public class StartCommand1 : BaseSagaStartCommand
    {
        public Guid Id { get; set; }
        public int Prop1 { get; set; }
        public bool Prop2 { get; set; }
    }

    public class BaseSagaStartCommand
    {
        protected BaseSagaStartCommand()
        {
            if (SagaId == null || SagaId.GetValueOrDefault() == Guid.Empty)
                SagaId = Guid.NewGuid();
        }

        public Guid? SagaId { get; set; }
    }

    public class DoWorkCommand
    {
        public Guid SagaId { get; set; }
    }

    public class DoWorkCompleteCommand
    {
        public Guid SagaId { get; set; }
    }

    public class SagaData1 : ISagaData
    {
        public Guid Id { get; set; }
        public int Revision { get; set; }

        public string SagaProp1 { get; set; }
        public StartCommand1 Command { get; set; }
    }
}