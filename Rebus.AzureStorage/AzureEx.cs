using System.Net;
using Microsoft.WindowsAzure.Storage;

namespace Rebus.AzureStorage
{
    static class AzureEx
    {
        public static bool IsStatus(this StorageException exception, HttpStatusCode statusCode)
        {
            var webException = exception.InnerException as WebException;
            var response = webException?.Response as HttpWebResponse;
            return response?.StatusCode == statusCode;
        }
    }
}