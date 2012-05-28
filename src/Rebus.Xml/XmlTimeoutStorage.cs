using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rebus.Timeout;

namespace Rebus.Xml
{
    public class XmlTimeoutStorage : IStoreTimeouts
    {
        public void Add(Timeout.Timeout newTimeout)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Timeout.Timeout> RemoveDueTimeouts()
        {
            throw new NotImplementedException();
        }
    }
}
