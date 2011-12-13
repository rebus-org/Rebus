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
using System.Diagnostics;

namespace Rebus.Logging
{
    public class TraceLoggerFactory : IRebusLoggerFactory
    {
        public ILog GetLogger(Type type)
        {
            return new TraceLogger(type);
        }

        class TraceLogger : ILog
        {
            readonly Type type;

            public TraceLogger(Type type)
            {
                this.type = type;
            }

            public void Debug(string message, params object[] objs)
            {
                Trace.TraceInformation(type + ": " + message, objs);
            }

            public void Info(string message, params object[] objs)
            {
                Trace.TraceInformation(type + ": " + message, objs);
            }

            public void Warn(string message, params object[] objs)
            {
                Trace.TraceWarning(type + ": " + message, objs);
            }

            public void Error(Exception exception, string message, params object[] objs)
            {
                Trace.TraceError(type + ": " + string.Format(message, objs) + Environment.NewLine + exception);
            }

            public void Error(string message, params object[] objs)
            {
                Trace.TraceError(type + ": " + message, objs);
            }
        }
    }
}