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

            using(var messageQueue = new MessageQueue(path))
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

        static QueueInfo Parse(string queueName)
        {
            if (queueName.Contains("@"))
            {
                var tokens = queueName.Split('@');
                if (tokens.Length != 2)
                {
                    throw new ArgumentException(string.Format("The specified MSMQ input queue is invalid!: {0}", queueName));
                }
                return new QueueInfo {QueueName = tokens[0], MachineName = tokens[1]};
            }
            return new QueueInfo {QueueName = queueName};
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

Everything around Rebus is built with the assumption that queues are transactional,
so Rebus will malfunction if queues aren't transactional. 

To remedy this, ensure that any existing queues are transactional, or let Rebus 
create its queues automatically.",
                            path);
                    throw new InvalidOperationException(message);
                }
            }
        }

        public static void EnsureMessageQueueExists(string path)
        {
            if (MessageQueue.Exists(path)) return;

            log.Info("MSMQ queue {0} does not exist - it will be created now...", path);

            var administratorAccountName = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)
                .Translate(typeof(NTAccount))
                .ToString();

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
                          "Could not create message queue {0} and grant FullControl permissions to {1} - deleting queue again to avoid dangling queues...",
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
            }
        }
    }
}