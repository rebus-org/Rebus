//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Blob;
//using Microsoft.WindowsAzure.Storage.RetryPolicies;
//using Rebus.Exceptions;

//namespace Rebus.AzureStorage.Sagas
//{
//    public class SagaLease : IDisposable
//    {
//        private CloudBlockBlob _dataBlob;
//        private string _leaseId;
//        private SagaLease() { }
//        public static async Task<SagaLease> AcquireAsync(CloudBlobContainer container, Guid sagaId, int revision)
//        {
//            var me = new SagaLease();
//            var dataRef = $"{sagaId}/data.json";
//            me._dataBlob = container.GetBlockBlobReference(dataRef);
//            if (!await me._dataBlob.ExistsAsync())
//            {
//                if (revision != 0)
//                {
//                    throw new ConcurrencyException("Saga Not found Saga {0}, Revision {1}", sagaId, revision);
//                }
//                await me._dataBlob.UploadTextAsync("A");
//                var exists = await me._dataBlob.ExistsAsync();
//            }

//            bool worked = false;
//            while (!worked)
//            {
//                try
//                {
//                    me._leaseId =
//                        await
//                            me._dataBlob.AcquireLeaseAsync(null);
                    
//                    worked = !String.IsNullOrEmpty(me._leaseId);
//                }
//                catch (StorageException ex)
//                {
//                    // 409 gets thrown when we try to get a lease that exists
//                    if (ex.RequestInformation.HttpStatusCode != 409)
//                    {
//                        throw;
//                    }
//                }
//            }
//            if (revision != 0 && me._dataBlob.Metadata["revision"] != "0")
//            {
//                throw new ConcurrencyException("Error leasing saga {0} for revision {1} existing blob had revision {2}", sagaId, revision, me._dataBlob.Metadata["revision"]);
//            }
//            return me;

//        }
//        public CloudBlockBlob Blob { get {  return _dataBlob;} }
//        public string LeaseId { get { return _leaseId; } }
//        public void Dispose()
//        {
            
//            _dataBlob.ReleaseLease(AccessCondition.GenerateLeaseCondition(_leaseId),

//                                new BlobRequestOptions { RetryPolicy = new ExponentialRetry() }, new OperationContext());
//        }
//    }
//}
