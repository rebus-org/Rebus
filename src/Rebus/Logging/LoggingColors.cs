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
    public class LoggingColors
    {
        public LoggingColors()
        {
            Debug = ColorSetting.Foreground(ConsoleColor.Gray);
            Info = ColorSetting.Foreground(ConsoleColor.Green);
            Warn = ColorSetting.Foreground(ConsoleColor.Yellow);
            Error = ColorSetting.Foreground(ConsoleColor.Red);
        }

        public ColorSetting Debug { get; set; }

        public ColorSetting Info { get; set; }
        
        public ColorSetting Warn { get; set; }
        
        public ColorSetting Error { get; set; }
    }
}