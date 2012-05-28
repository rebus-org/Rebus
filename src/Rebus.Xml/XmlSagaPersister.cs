using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace Rebus.Xml
{
    /// <summary>
    /// Class for storing Rebus sagas to XML
    /// </summary>
    public class XmlSagaPersister : IStoreSagaData
    {
        private static readonly object _o = new object();
        private readonly string _filePath;

        /// <summary>
        /// Creates a new instance of the XML sage persister
        /// </summary>
        /// <param name="filePath">Full path to target file for storing sagas. Must have write access!</param>
        public XmlSagaPersister(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");

            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _filePath = filePath;
        }

        public void Delete(ISagaData sagaData)
        {
            throw new NotImplementedException();

            if (sagaData == null)
                throw new ArgumentNullException("sagaData");

            lock (_o)
            {
                var oldDoc = GetSagaDocument();
                var existingSagas = GetSagas(oldDoc);
                var newSagas = from s in existingSagas
                               where s.Id != sagaData.Id
                               select s;
                var newDoc = CreateEmptySagaDocument();
                newDoc.Root.Add(from s in newSagas
                                select CreateSagaElement(s));
                newDoc.Save(_filePath);
            }
        }

        public T Find<T>(string sagaDataPropertyPath, object fieldFromMessage) where T : ISagaData
        {
            throw new NotImplementedException();
        }

        public void Insert(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            throw new NotImplementedException();

            if (sagaData == null)
                throw new ArgumentNullException("sagaData");

            lock (_o)
            {
                var doc = GetSagaDocument();
                doc.Root.Add(CreateSagaElement(sagaData));
                doc.Save(_filePath);
            }
        }

        public void Update(ISagaData sagaData, string[] sagaDataPropertyPathsToIndex)
        {
            throw new NotImplementedException();

            if (sagaData == null)
                throw new ArgumentNullException("sagaData");

            lock (_o)
            {
                var oldDoc = GetSagaDocument();
                var existingSagas = GetSagas(oldDoc);
                var newSagas = from s in existingSagas
                               where s.Id != sagaData.Id
                               select s;
                var newDoc = CreateEmptySagaDocument();
                newDoc.Root.Add(from s in newSagas
                                select CreateSagaElement(s));
                newDoc.Root.Add(CreateSagaElement(sagaData));
                newDoc.Save(_filePath);
            }
        }

        private IEnumerable<ISagaData> GetSagas(XDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException("doc");

            return from s in doc.Descendants("saga")
                   select new SagaData { Id = Guid.Parse(s.Element("id").Value), Revision = int.Parse(s.Element("revision").Value) };
        }

        private XElement CreateSagaElement(ISagaData sagaData)
        {
            return new XElement(
                "saga",
                    new XElement("id", sagaData.Id),
                    new XElement("revision", sagaData.Revision)
                );
        }

        private XDocument GetSagaDocument()
        {
            if (File.Exists(_filePath))
                return XDocument.Load(_filePath);

            return CreateEmptySagaDocument();
        }

        private XDocument CreateEmptySagaDocument()
        {
            var doc = new XDocument();
            var root = new XElement("sagas");
            doc.Add(root);
            return doc;
        }

        class SagaData : ISagaData
        {
            public Guid Id { get; set; }
            public int Revision { get; set; }
        }

        public void Clear()
        {
            lock (_o)
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
        }
    }
}
