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
using log4net;
using RebusLog=Rebus.Logging.ILog;

namespace Rebus.Log4Net
{
    class Log4NetLogger : RebusLog
    {
        readonly ILog log;

        public Log4NetLogger(ILog log)
        {
            this.log = log;
        }

        public void Debug(string message, params object[] objs)
        {
            log.DebugFormat(message, objs);
        }

        public void Info(string message, params object[] objs)
        {
            log.InfoFormat(message, objs);
        }

        public void Warn(string message, params object[] objs)
        {
            log.WarnFormat(message, objs);
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            try
            {
                log.Error(string.Format(message, objs), exception);
            }
            catch
            {
                log.WarnFormat("Could not render string with arguments: {0}", message);
                log.Error(message, exception);
            }
        }

        public void Error(string message, params object[] objs)
        {
            log.ErrorFormat(message, objs);
        }
    }
}
