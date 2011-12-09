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
    public class ColorSetting
    {
        ColorSetting(ConsoleColor foregroundColor)
        {
            ForegroundColor = foregroundColor;
        }

        public ConsoleColor ForegroundColor { get; set; }
        
        public ConsoleColor? BackgroundColor { get; set; }

        public static ColorSetting Foreground(ConsoleColor foregroundColor)
        {
            return new ColorSetting(foregroundColor);
        }

        public ColorSetting Background(ConsoleColor backgroundColor)
        {
            BackgroundColor = backgroundColor;
            return this;
        }

        public IDisposable Enter()
        {
            return new ConsoleColorContext(this);
        }

        class ConsoleColorContext : IDisposable
        {
            public ConsoleColorContext(ColorSetting colorSetting)
            {
                if (colorSetting.BackgroundColor.HasValue)
                {
                    Console.BackgroundColor = colorSetting.BackgroundColor.Value;
                }

                Console.ForegroundColor = colorSetting.ForegroundColor;
            }

            public void Dispose()
            {
                Console.ResetColor();
            }
        }
    }
}