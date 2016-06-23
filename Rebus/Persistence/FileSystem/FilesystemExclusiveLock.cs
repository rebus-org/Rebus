using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.Persistence.FileSystem
{
    public class FilesystemExclusiveLock : IDisposable
    {
        private readonly FileStream _fileStream;
        public FilesystemExclusiveLock(string pathToLock)
        {
            EnsureTargetFile(pathToLock);
            bool success = false;

            //Unfortunately this is the only filesystem locking api that .net exposes
            //You can P/Invoke into better ones but thats not cross-platform
            while (!success)
            {
                try
                {
                    _fileStream = new FileStream(pathToLock, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    // Oh and there's no async version!
                    _fileStream.Lock(0, 1);
                    success = true;
                }
                catch (IOException ex)
                {
                    success = false;
                    //Have I mentioned that I hate this algorithm?
                    //This basically just causes the thread to yield to the scheduler
                    //we'll be back here more than 1 tick from now
                    System.Threading.Thread.Sleep(TimeSpan.FromTicks(1));
                }
            }
        }

        private void EnsureTargetFile(string pathToLock)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(pathToLock)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathToLock));
                }
            }
            catch (IOException ex)
            {
                //Someone else did this under us
            }
            try
            {
                if (!File.Exists(pathToLock))
                {
                    File.WriteAllText(pathToLock, "A");
                }
            }
            catch (IOException ex)
            {
                //Someone else did this under us
            }
        }

        public void Dispose()
        {
            _fileStream.Unlock(0,1);
        }
    }
}
