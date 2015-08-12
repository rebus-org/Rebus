using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ponder;

namespace Rebus.Tests.Performance
{
    public static class SagaPersisterPerformanceTestHelper
    {
        public static void DoTheTest(IStoreSagaData persister, int numberOfSagas, int iterations)
        {
            var sagaDatas = Enumerable.Range(0, numberOfSagas)
                .Select(i => new SomePieceOfFairlyComplexSagaData
                                 {
                                     Id = Guid.NewGuid(),
                                     EmbeddedThings =
                                         {
                                             new SomeEmbeddedThing(),
                                             new SomeEmbeddedThing(),
                                             new SomeEmbeddedThing(),
                                             new SomeEmbeddedThing(),
                                             new SomeEmbeddedThing(),
                                             new SomeEmbeddedThing(),
                                         },
                                     AnotherEmbeddedThing =
                                         new AnotherEmbeddedThing
                                             {
                                                 Leafs =
                                                     {
                                                         new LeafThing(),
                                                         new LeafThing(),
                                                         new LeafThing(),
                                                         new LeafThing(),
                                                     }
                                             },
                                     OrdinaryField = Guid.NewGuid().ToString(),
                                     YetAnotherEmbeddedThing = new YetAnotherEmbeddedThing
                                                                   {
                                                                       EvenDeeperEmbeddedThing =
                                                                           new EvenDeeperEmbeddedThing()
                                                                   }
                                 })
                .ToList();

            var pathsToIndex =
                new[]
                    {
                        Reflect.Path<SomePieceOfFairlyComplexSagaData>(d => d.Id),
                        Reflect.Path<SomePieceOfFairlyComplexSagaData>(d => d.OrdinaryField),
                        Reflect.Path<SomePieceOfFairlyComplexSagaData>(d => d.AnotherEmbeddedThing.EmbeddedValue),
                        Reflect.Path<SomePieceOfFairlyComplexSagaData>(d => d.YetAnotherEmbeddedThing.EvenDeeperEmbeddedThing.FinallySomeValue),
                    };

            Console.WriteLine("Running {0} iterations of saving/updating {1} sagas", iterations, numberOfSagas);

            var stopwatch = Stopwatch.StartNew();
            for (var counter = 0; counter < iterations; counter++)
            {
                foreach (var data in sagaDatas)
                {
                    if (counter == 0)
                    {
                        persister.Insert(data, pathsToIndex);
                    }
                    else
                    {
                        persister.Update(data, pathsToIndex);
                    }
                }
            }

            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(@"Saving/updating {0} sagas {1} times took {2:0.0} s - that's {3:0} ops/s",
                              numberOfSagas,
                              iterations,
                              elapsed.TotalSeconds,
                              numberOfSagas*iterations/elapsed.TotalSeconds);
        }

        internal class SomePieceOfFairlyComplexSagaData : ISagaData
        {
            public SomePieceOfFairlyComplexSagaData()
            {
                EmbeddedThings = new List<SomeEmbeddedThing>();
                OrdinaryField = Guid.NewGuid().ToString();
            }

            public Guid Id { get; set; }

            public int Revision { get; set; }

            public List<SomeEmbeddedThing> EmbeddedThings { get; set; }

            public string OrdinaryField { get; set; }

            public AnotherEmbeddedThing AnotherEmbeddedThing { get; set; }

            public YetAnotherEmbeddedThing YetAnotherEmbeddedThing { get; set; }
        }

        internal class SomeEmbeddedThing
        {
            public SomeEmbeddedThing()
            {
                SomeValue = Guid.NewGuid().ToString();
            }

            public string SomeValue { get; set; }
        }

        internal class AnotherEmbeddedThing
        {
            public AnotherEmbeddedThing()
            {
                EmbeddedValue = Guid.NewGuid().ToString();
                Leafs = new List<LeafThing>();
            }

            public List<LeafThing> Leafs { get; set; }

            public string EmbeddedValue { get; set; }
        }

        internal class LeafThing
        {
            public LeafThing()
            {
                SomeValue = Guid.NewGuid().ToString();
            }

            public string SomeValue { get; set; }
        }

        internal class YetAnotherEmbeddedThing
        {
            public EvenDeeperEmbeddedThing EvenDeeperEmbeddedThing { get; set; }
        }

        internal class EvenDeeperEmbeddedThing
        {
            public EvenDeeperEmbeddedThing()
            {
                FinallySomeValue = Guid.NewGuid().ToString();
            }

            public string FinallySomeValue { get; set; }
        }
    }
}