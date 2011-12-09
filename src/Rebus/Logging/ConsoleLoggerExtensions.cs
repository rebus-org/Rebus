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
using Rebus.Configuration.Configurers;

namespace Rebus.Logging
{
    public static class ConsoleLoggerExtensions
    {
        /// <summary>
        /// Use console stdout for logging (probably only useful for debugging and test scenarios)
        /// </summary>
        public static void Console(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(colored: false);
        }

        /// <summary>
        /// Use colored console stdout for logging (probably only useful for debugging and test scenarios)
        /// </summary>
        public static void ColoredConsole(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(colored: true);
        }

        /// <summary>
        /// Use colored console stdout for logging (probably only useful for debugging and test scenarios)
        /// and allow the colors to be customized
        /// </summary>
        public static void ColoredConsole(this LoggingConfigurer configurer, LoggingColors colors)
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(colored: true) {Colors = colors};
        }

        /// <summary>
        /// Use the .NET's <see cref="System.Diagnostics.Trace"/> for logging.
        /// </summary>
        public static void Trace(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new TraceLoggerFactory();
        }

        /// <summary>
        /// Disables logging completely. Why would you do that?
        /// </summary>
        public static void None(this LoggingConfigurer configurer)
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();
        }
    }
}