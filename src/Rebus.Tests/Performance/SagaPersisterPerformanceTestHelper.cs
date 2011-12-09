// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
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
                        Reflect.Path<SomePieceOfFairlyComplexSagaData>(
                            d => d.YetAnotherEmbeddedThing.EvenDeeperEmbeddedThing.FinallySomeValue),
                    };

            Console.WriteLine("Running {0} iterations of saving/updating {1} sagas", iterations, numberOfSagas);

            var stopwatch = Stopwatch.StartNew();
            for (var counter = 0; counter < iterations; counter++)
            {
                foreach (var data in sagaDatas)
                {
                    persister.Save(data, pathsToIndex);
                }
            }

            var elapsed = stopwatch.Elapsed;
            Console.WriteLine(@"Saving/updating {0} sagas {1} times took {2:0.0} s - that's {3:0} ops/s",
                              numberOfSagas,
                              iterations,
                              elapsed.TotalSeconds,
                              numberOfSagas*iterations/elapsed.TotalSeconds);
        }

        class SomePieceOfFairlyComplexSagaData : ISagaData
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

        class SomeEmbeddedThing
        {
            public SomeEmbeddedThing()
            {
                SomeValue = Guid.NewGuid().ToString();
            }

            public string SomeValue { get; set; }
        }

        class AnotherEmbeddedThing
        {
            public AnotherEmbeddedThing()
            {
                EmbeddedValue = Guid.NewGuid().ToString();
                Leafs = new List<LeafThing>();
            }

            public List<LeafThing> Leafs { get; set; }

            public string EmbeddedValue { get; set; }
        }

        class LeafThing
        {
            public LeafThing()
            {
                SomeValue = Guid.NewGuid().ToString();
            }

            public string SomeValue { get; set; }
        }

        class YetAnotherEmbeddedThing
        {
            public EvenDeeperEmbeddedThing EvenDeeperEmbeddedThing { get; set; }
        }

        class EvenDeeperEmbeddedThing
        {
            public EvenDeeperEmbeddedThing()
            {
                FinallySomeValue = Guid.NewGuid().ToString();
            }

            public string FinallySomeValue { get; set; }
        }

    }
}