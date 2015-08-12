using System;
using System.Messaging;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using Rebus.Logging;

namespace Rebus.Shared
{
    static internal class MsmqUtil
    {
        static ILog log;

        static MsmqUtil()
        {
            RebusLoggerFactory.Changed += f => log = f.GetCurrentClassLogger();
        }

        public static void PurgeQueue(string queueName)
        {
            var path = GetPath(queueName);

            if (!MessageQueue.Exists(path)) return;

            using (var messageQueue = new MessageQueue(path))
            {
                messageQueue.Purge();
            }
        }

        public static string GetPath(string queueName)
        {
            var bim = Parse(queueName);

            return GenerateSimplePath(bim.MachineName ?? ".", bim.QueueName);
        }

        public static string GetFullPath(string queueName)
        {
            var bim = Parse(queueName);

            return GenerateFullPath(bim.MachineName ?? Environment.MachineName, bim.QueueName);
        }

        public static string GetSenderPath(string destinationQueueName)
        {
            var queueInfo = Parse(destinationQueueName);

            if (string.IsNullOrEmpty(queueInfo.MachineName) || queueInfo.MachineName == Environment.MachineName)
            {
                return GenerateSimplePath(".", queueInfo.QueueName);
            }

            return GetFullPath(destinationQueueName);
        }

        public static string GenerateFullPath(string machineName, string queueName)
        {
            if (IsIpAddress(machineName))
            {
                return string.Format(@"FormatName:DIRECT=TCP:{0}\private$\{1}", machineName.ToLower(), queueName);
            }

            return string.Format(@"FormatName:DIRECT=OS:{0}\private$\{1}", machineName.ToLower(), queueName);
        }

        static string GenerateSimplePath(string machineName, string queueName)
        {
            return string.Format(@"{0}\private$\{1}", machineName, queueName);
        }

        static bool IsIpAddress(string machineName)
        {
            var ipTokens = machineName.Split('.');

            return ipTokens.Length == 4 && ipTokens.All(IsByte);
        }

        static bool IsByte(string str)
        {
            byte temp;
            return byte.TryParse(str, out temp);
        }

        class QueueInfo
        {
            public string MachineName { get; set; }
            public string QueueName { get; set; }
        }

        public static bool QueueExists(string queueName)
        {
            return MessageQueue.Exists(GetPath(queueName));
        }

        public static void Delete(string queueName)
        {
            var queuePath = GetPath(queueName);
            if (!MessageQueue.Exists(queuePath)) return;

            MessageQueue.Delete(queuePath);
        }

        static QueueInfo Parse(string queueName)
        {
            if (queueName.Contains("@"))
            {
                var tokens = queueName.Split('@');
                if (tokens.Length != 2)
                {
                    throw new ArgumentException(string.Format(@"The specified MSMQ queue name is invalid!: {0} - please format queue names according to one of the following examples:

    someQueue

        in order to specify a private queue named 'someQueue' on the local machine, or

    someQueue@anotherMachine

        in order to specify a private queue named 'someQueue' on the machine with hostname 'anotherMachine', or

    someQueue@10.0.1.45

        in order to specify a private queue named 'someQueue' on the machine with IP 10.0.1.45", queueName));
                }
                return new QueueInfo { QueueName = tokens[0], MachineName = tokens[1] };
            }
            return new QueueInfo { QueueName = queueName };
        }

        public static void EnsureMessageQueueIsTransactional(string path)
        {
            using (var queue = new MessageQueue(path))
            {
                if (!queue.Transactional)
                {
                    var message =
                        string.Format(
                            @"The queue {0} is NOT transactional!

Everything around Rebus is built with the assumption that queues are transactional, so Rebus will malfunction if queues aren't transactional. 

To remedy this, ensure that any existing queues are transactional, or let Rebus create its queues automatically.

If Rebus allowed you to work with non-transactional queues, it would not be able to e.g. safely move a received message into an error queue. Also, MSMQ does not behave well when moving messages in/out of transactional/non-transactional queue.",
                            path);
                    throw new InvalidOperationException(message);
                }
            }
        }

        public static void EnsureMessageQueueExists(string path)
        {
            try
            {
                if (MessageQueue.Exists(path)) return;
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    string.Format("An exception occurred while checking whether the queue with the path {0} exists",
                                  path), e);
            }

            log.Info("MSMQ queue {0} does not exist - it will be created now...", path);

            var administratorAccountName = GetAdministratorAccountName();

            try
            {
                using (var messageQueue = MessageQueue.Create(path, true))
                {
                    messageQueue.SetPermissions(Thread.CurrentPrincipal.Identity.Name,
                                                MessageQueueAccessRights.GenericWrite);

                    messageQueue.SetPermissions(administratorAccountName, MessageQueueAccessRights.FullControl);
                }
            }
            catch (Exception e)
            {
                log.Error(e,
                          "Could not create message queue {0} and grant FullControl permissions to {1} - will attempt to delete the queue again to avoid dangling queues with broken access rights...",
                          path,
                          administratorAccountName);
                try
                {
                    MessageQueue.Delete(path);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Could not delete queue {0}", path);
                }

                throw;
            }
        }

        static string GetAdministratorAccountName()
        {
            try
            {
                return new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)
                    .Translate(typeof(NTAccount))
                    .ToString();
            }
            catch (Exception e)
            {
                throw new ApplicationException(
                    string.Format(
                        "An error occurred while attempting to figure out the name of the local administrators group!"),
                    e);
            }
        }

        public static bool IsLocal(string errorQueueName)
        {
            var queueInfo = Parse(errorQueueName);

            return queueInfo.MachineName == null
                   || queueInfo.MachineName == "."
                   || queueInfo.MachineName.ToLowerInvariant().Equals(Environment.MachineName);
        }
    }
}