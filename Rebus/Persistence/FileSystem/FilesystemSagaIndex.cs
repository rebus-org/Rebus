using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Rebus.Sagas;

namespace Rebus.Persistence.FileSystem
{
    internal class FilesystemSagaIndex 
    {
        private readonly string _basePath;



        readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        public FilesystemSagaIndex(string basePath)
        {


            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            _basePath = basePath;
            

        }
        private string IndexPath { get { return Path.Combine(_basePath, "index.json"); } }
        private List<SagaIndexItem> ReadIndexItems()
        {

            if (File.Exists(IndexPath))
            {
                var readAllText = File.ReadAllText(IndexPath);
                var readIndexItems = JsonConvert.DeserializeObject<List<SagaIndexItem>>(readAllText);
                return readIndexItems;
            }
            return new List<SagaIndexItem>();
        }

        private void WriteSagaIndexItems(List<SagaIndexItem> _indexItems )
        {
            var serializeObject = JsonConvert.SerializeObject(_indexItems);
            File.WriteAllText(IndexPath, serializeObject);
        }


        public ISagaData Find(Type sagaDataType, string propertyName, object propertyValue)
        {
            var _indexItems = ReadIndexItems();
            var item = _indexItems
                .FirstOrDefault(i => i.SagaType == sagaDataType.FullName && i.PropertyName == propertyName && i.PropertyValue == propertyValue?.ToString());
            if (item == null) return null;
            return Find(item.SagaId);
        }

        public ISagaData Find(Guid id)
        {
            var filePath = SagaFilePath(id);
            if (!File.Exists(filePath)) return null;
            var data = File.ReadAllText(filePath);
            return (ISagaData)JsonConvert.DeserializeObject(data, _serializerSettings);
        }

        private string SagaFilePath(Guid id)
        {
            var filePath = Path.Combine(_basePath, $"{id:N}.json");
            return filePath;
        }

        public void Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            var _indexItems = ReadIndexItems();
            _indexItems.RemoveAll(i => i.SagaId == sagaData.Id);
            _indexItems.AddRange(GetPropertiesToIndex(sagaData, correlationProperties));
            var path = SagaFilePath(sagaData.Id);
            File.WriteAllText(path, JsonConvert.SerializeObject(sagaData, _serializerSettings));
            WriteSagaIndexItems(_indexItems);
        }

        static IEnumerable<SagaIndexItem> GetPropertiesToIndex(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
        {
            return correlationProperties
                .Select(p => p.PropertyName)
                .Select(path => new SagaIndexItem
                {
                    SagaType = sagaData.GetType().FullName,
                    SagaId = sagaData.Id,
                    PropertyName = path,
                    PropertyValue = Value(sagaData, path)?.ToString()

                });
        }
        public static object Value(object obj, string path)
        {
            var dots = path.Split('.');

            foreach (var dot in dots)
            {
                var propertyInfo = obj.GetType().GetProperty(dot);
                if (propertyInfo == null) return null;
                obj = propertyInfo.GetValue(obj, new object[0]);
                if (obj == null) break;
            }

            return obj;
        }

        public bool Contains(Guid id)
        {
            return File.Exists(SagaFilePath(id));
        }

        public void Remove(Guid sagaDataId)
        {
            var _indexItems = ReadIndexItems();
            _indexItems.RemoveAll(i => i.SagaId == sagaDataId);
            var path = SagaFilePath(sagaDataId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            WriteSagaIndexItems(_indexItems);
        }
        public class SagaIndexItem
        {
            public string SagaType { get; set; }
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
            public Guid SagaId { get; set; }
        }
    }
}
