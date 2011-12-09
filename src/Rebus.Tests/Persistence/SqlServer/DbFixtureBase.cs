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
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using NUnit.Framework;
using log4net.Config;

namespace Rebus.Tests.Persistence.SqlServer
{
    public class DbFixtureBase
    {
        static DbFixtureBase()
        {
            XmlConfigurator.Configure();
        }

        [SetUp]
        public void SetUp()
        {
            DoSetUp();
        }

        protected virtual void DoSetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
            DoTearDown();
        }

        protected virtual void DoTearDown()
        {
        }

        protected static string ConnectionString
        {
            get
            {
                var connectionStringSettings = ConfigurationManager.ConnectionStrings
                        .Cast<ConnectionStringSettings>()
                        .FirstOrDefault();

                Assert.IsNotNull(connectionStringSettings,
                                 "There doesn't seem to be any connection strings in the app.config.");

                return connectionStringSettings.ConnectionString;
            }
        }

        protected void DeleteRows(string tableName)
        {
            ExecuteCommand("delete from " + tableName);
        }

        static void ExecuteCommand(string commandText)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}