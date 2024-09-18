using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Rebus.Sagas;

namespace Rebus.Persistence.FileSystem;

sealed class FileSystemSagaIndex
{
    readonly string _basePath;

    readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public FileSystemSagaIndex(string basePath)
    {
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
        _basePath = basePath;
    }

    string IndexPath => Path.Combine(_basePath, "index.json");

    public ISagaData FindById(Guid id)
    {
        var filePath = SagaFilePath(id);
        if (!File.Exists(filePath)) return null;
        var data = File.ReadAllText(filePath);
        return (ISagaData)JsonConvert.DeserializeObject(data, _serializerSettings);
    }

    public ISagaData Find(Type sagaDataType, string propertyName, object propertyValue)
    {
        var indexItems = ReadIndexItems();

        var item = indexItems
            .Find(i => i.SagaType == sagaDataType.FullName
                                 && i.PropertyName == propertyName
                                 && i.PropertyValue == propertyValue?.ToString());

        return item == null ? null : FindById(item.SagaId);
    }

    public void Insert(ISagaData sagaData, IEnumerable<ISagaCorrelationProperty> correlationProperties)
    {
        var indexItems = ReadIndexItems();
        indexItems.RemoveAll(i => i.SagaId == sagaData.Id);
        indexItems.AddRange(GetPropertiesToIndex(sagaData, correlationProperties));
        var path = SagaFilePath(sagaData.Id);
        File.WriteAllText(path, JsonConvert.SerializeObject(sagaData, _serializerSettings));
        WriteSagaIndexItems(indexItems);
    }

    public bool Contains(Guid id)
    {
        return File.Exists(SagaFilePath(id));
    }

    public void Remove(Guid sagaDataId)
    {
        var indexItems = ReadIndexItems();
        indexItems.RemoveAll(i => i.SagaId == sagaDataId);
        var path = SagaFilePath(sagaDataId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        WriteSagaIndexItems(indexItems);
    }

    List<SagaIndexItem> ReadIndexItems()
    {

        if (File.Exists(IndexPath))
        {
            var readAllText = File.ReadAllText(IndexPath);
            var readIndexItems = JsonConvert.DeserializeObject<List<SagaIndexItem>>(readAllText);
            return readIndexItems;
        }
        return new List<SagaIndexItem>();
    }

    void WriteSagaIndexItems(List<SagaIndexItem> indexItems)
    {
        var serializeObject = JsonConvert.SerializeObject(indexItems);
        File.WriteAllText(IndexPath, serializeObject);
    }

    string SagaFilePath(Guid id)
    {
        var filePath = Path.Combine(_basePath, $"{id:N}.json");
        return filePath;
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

    static object Value(object obj, string path)
    {
        var dots = path.Split('.');

        foreach (var dot in dots)
        {
            var propertyInfo = obj.GetType().GetProperty(dot);
            if (propertyInfo == null) return null;
            obj = propertyInfo.GetValue(obj, Array.Empty<object>());
            if (obj == null) break;
        }

        return obj;
    }

    sealed class SagaIndexItem
    {
        public string SagaType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public Guid SagaId { get; set; }
    }
}