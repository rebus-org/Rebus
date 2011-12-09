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
using System.IO;

namespace Rebus.Configuration
{
    /// <summary>
    /// <see cref="IAppConfigLoader"/> that can load the application configuration 
    /// file of the currently activated AppDomain.
    /// </summary>
    public class StandardAppConfigLoader : IAppConfigLoader
    {
        public string LoadIt()
        {
            var pathToAppConfig = AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE") as string;

            return string.IsNullOrEmpty(pathToAppConfig)
                       ? ""
                       : File.ReadAllText(pathToAppConfig);
        }
    }
}