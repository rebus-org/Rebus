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

namespace Rebus.Logging
{
    public class RebusLoggerFactory
    {
        static readonly IRebusLoggerFactory Default = new ConsoleLoggerFactory(colored: true);
        static IRebusLoggerFactory current = Default;

        public static IRebusLoggerFactory Current
        {
            get { return current; }
            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException(string.Format(@"Cannot set current IRebusLoggerFactory to null! 

If you want to disable logging completely, you can set Current to an instance of NullLoggerFactory.

Alternatively, if you're using the configuration API, you can disable logging like so:

    Configure.With(myAdapter)
        .Logging(l => l.None())
        .(...)

"));
                }

                current = value;
            }
        }

        public static ILog GetLogger(Type type)
        {
            return Current.GetLogger(type);
        }

        public static void Reset()
        {
            current = Default;
        }
    }
}