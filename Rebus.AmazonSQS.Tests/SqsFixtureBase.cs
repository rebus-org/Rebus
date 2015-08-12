using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Tests;
using Rebus.Transport;

namespace Rebus.AmazonSQS.Tests
{
    public abstract class SqsFixtureBase : FixtureBase
    {
        readonly Encoding _defaultEncoding = Encoding.UTF8;


        protected async Task WithContext(Func<ITransactionContext, Task> contextAction, bool completeTransaction = true)
        {
            using (var context = new DefaultTransactionContext())
            {
                await contextAction(context);

                if (completeTransaction)
                {
                    await context.Complete();
                }
            }
        }

        protected string GetStringBody(TransportMessage transportMessage)
        {
            if (transportMessage == null)
            {
                throw new InvalidOperationException("Cannot get string body out of null message!");
            }

            return _defaultEncoding.GetString(transportMessage.Body);
        }

        protected TransportMessage MessageWith(string stringBody)
        {
            var headers = new Dictionary<string, string>
                          {
                              {Headers.MessageId, Guid.NewGuid().ToString()}
                          };
            var body = _defaultEncoding.GetBytes(stringBody);
            return new TransportMessage(headers, body);
        }
    }
}
