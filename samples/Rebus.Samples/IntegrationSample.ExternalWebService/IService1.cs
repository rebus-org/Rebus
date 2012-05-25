using System.ServiceModel;

namespace IntegrationSample.ExternalWebService
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        string GetGreeting();
    }
}
