using System;
using System.Diagnostics;
using System.IO;
using MongoDB.Driver;

namespace Rebus.Tests.Persistence
{
    public class MongoHelper
    {
        static string Datapath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mongodata"); }
        }

        public static Process StartServerFromScratch()
        {
            if (Directory.Exists(Datapath))
                Directory.Delete(Datapath, true);

            Directory.CreateDirectory(Datapath);

            var start = new ProcessStartInfo
            {
                FileName = @"..\..\..\..\tools\mongodb\bin\mongod.exe",
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = @"--port 27018 --dbpath " + Datapath
            };

            return Process.Start(start);
        }

        public static void StopServer(Process mongod, MongoDatabase db)
        {
            db.Server.Shutdown();
            mongod.Kill();
            mongod.WaitForExit();
            Directory.Delete(Datapath, true);
        }

        public static MongoDatabase GetDatabase(string connectionString)
        {
            var mongoUrl = new MongoUrl(connectionString);
            
            return new MongoClient(mongoUrl)
                .GetServer()
                .GetDatabase(mongoUrl.DatabaseName);
        }
    }
}