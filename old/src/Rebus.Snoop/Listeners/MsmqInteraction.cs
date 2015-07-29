using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Rebus.Shared;
using Rebus.Snoop.Events;
using Rebus.Snoop.ViewModel.Models;
using Message = Rebus.Snoop.ViewModel.Models.Message;

namespace Rebus.Snoop.Listeners
{
    public class MsmqInteraction
    {
        public MsmqInteraction()
        {
            Messenger.Default.Register(this, (MachineAdded newMachineCreated) => LoadQueues(newMachineCreated.Machine));
            Messenger.Default.Register(this, (ReloadQueuesRequested request) => LoadQueues(request.Machine));
            Messenger.Default.Register(this, (ReloadMessagesRequested request) => LoadMessages(request.Queue));
            Messenger.Default.Register(this, (MoveMessagesToSourceQueueRequested request) => MoveMessagesToSourceQueues(request.MessagesToMove));
            Messenger.Default.Register(this, (MoveMessagesToQueueRequested request) => MoveMessagesToQueue(request.MessagesToMove, true));
            Messenger.Default.Register(this, (CopyMessagesToQueueRequested request) => MoveMessagesToQueue(request.MessagesToMove, false));
            Messenger.Default.Register(this, (DeleteMessagesRequested request) => DeleteMessages(request.MessagesToMove));
            Messenger.Default.Register(this, (DownloadMessagesRequested request) => DownloadMessages(request.MessagesToDownload));
            Messenger.Default.Register(this, (UpdateMessageRequested request) => UpdateMessage(request.Message, request.Queue));
            Messenger.Default.Register(this, (PurgeMessagesRequested request) => PurgeMessages(request.Queue));
            Messenger.Default.Register(this, (DeleteQueueRequested request) => DeleteQueue(request.Queue));
        }

        void DeleteQueue(Queue queue)
        {
            var isOk = IsOkWithUser("This will DELETE the queue {0} completely - press OK to continue...",
                                                         queue.QueueName);

            if (!isOk)
            {
                return;
            }

            Task.Factory
                .StartNew(() =>
                    {
                        try
                        {
                            MessageQueue.Delete(queue.QueuePath);

                            return new
                                       {
                                           Success = true,
                                           Notification = NotificationEvent.Success("Queue {0} was deleted", queue.QueueName)
                                       };
                        }
                        catch (Exception e)
                        {
                            return new
                                       {
                                           Success = false,
                                           Notification = NotificationEvent.Fail(e.ToString(),
                                                                                 "Something went wrong while attempting to delete queue {0}",
                                                                                 queue.QueueName),
                                       };
                        }
                    })
                .ContinueWith(r =>
                    {
                        var result = r.Result;

                        Messenger.Default.Send(result.Notification);

                        if (result.Success)
                        {
                            Messenger.Default.Send(new QueueDeleted(queue));
                        }
                    }, Context.UiThread);
        }

        void PurgeMessages(Queue queue)
        {
            var isOk = IsOkWithUser("This will delete all the messages from the queue {0} - press OK to continue...",
                                                         queue.QueueName);

            if (!isOk)
            {
                return;
            }

            Task.Factory
                .StartNew(() =>
                    {
                        try
                        {
                            using (var msmqQueue = new MessageQueue(queue.QueuePath))
                            {
                                msmqQueue.Purge();
                            }

                            return new
                                       {
                                           Notification =
                                               NotificationEvent.Success("Queue {0} was purged", queue.QueueName),
                                           Success = true,
                                       };
                        }
                        catch (Exception e)
                        {
                            return new
                                       {
                                           Notification = NotificationEvent.Fail(e.ToString(),
                                                                                 "Something went wrong while attempting to purge queue {0}",
                                                                                 queue.QueueName),
                                           Success = false,
                                       };
                        }
                    })
                .ContinueWith(r =>
                    {
                        var result = r.Result;

                        Messenger.Default.Send(result.Notification);

                        if (result.Success)
                        {
                            Messenger.Default.Send(new QueuePurged(queue));
                        }
                    }, Context.UiThread);
        }

        static bool IsOkWithUser(string question, params object[] objs)
        {
            var text = string.Format(question, objs);

            var messageBoxResult = MessageBox.Show(text, "Question", MessageBoxButton.OKCancel);

            return messageBoxResult == MessageBoxResult.OK;
        }

        void UpdateMessage(Message message, Queue queueToReload)
        {
            Task.Factory
                .StartNew(() =>
                    {
                        if (!message.CouldDeserializeBody)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "Body of message with ID {0} was not properly deserialized, so it's not safe to try to update it...",
                                    message.Id));
                        }

                        using (var queue = new MessageQueue(message.QueuePath))
                        {
                            queue.MessageReadPropertyFilter = LosslessFilter();
                            using (var transaction = new MessageQueueTransaction())
                            {
                                transaction.Begin();
                                try
                                {
                                    var msmqMessage = queue.ReceiveById(message.Id, transaction);

                                    var newMsmqMessage =
                                        new System.Messaging.Message
                                            {
                                                Label = msmqMessage.Label,
                                                Extension = msmqMessage.Extension,

                                                TimeToBeReceived = msmqMessage.TimeToBeReceived,
                                                UseDeadLetterQueue = msmqMessage.UseDeadLetterQueue,
                                                UseJournalQueue = msmqMessage.UseJournalQueue,
                                            };

                                    EncodeBody(newMsmqMessage, message);

                                    queue.Send(newMsmqMessage, transaction);

                                    transaction.Commit();
                                }
                                catch
                                {
                                    transaction.Abort();
                                    throw;
                                }
                            }
                        }

                        return new
                                   {
                                       Message = message,
                                       Queue = queueToReload,
                                       Notification =
                                           NotificationEvent.Success("Fetched message with ID {0} and put an updated version back in the queue", message.Id),
                                   };
                    })
                .ContinueWith(a =>
                    {
                        if (a.Exception != null)
                        {
                            Messenger.Default.Send(NotificationEvent.Fail(a.Exception.ToString(),
                                                                          "Something went wrong while attempting to update message with ID {0}",
                                                                          message.Id));

                            return;
                        }

                        var result = a.Result;
                        Messenger.Default.Send(new ReloadMessagesRequested(result.Queue));
                        Messenger.Default.Send(result.Notification);
                    }, Context.UiThread);
        }

        void DownloadMessages(List<Message> messages)
        {
            Task.Factory
                .StartNew(() =>
                    {
                        // for now, just settle with using the desktop
                        var directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                        Func<int, string> getName = i => Path.Combine("Snoop Message Export", string.Format("export-{0}", i));
                        var counter = 1;
                        var directoryName = getName(counter);
                        while (Directory.Exists(Path.Combine(directory, directoryName)))
                        {
                            directoryName = getName(++counter);
                        }

                        var directoryToSaveMessagesTo =
                            new DirectoryInfo(directory).CreateSubdirectory(directoryName);

                        return new
                                   {
                                       Directory = directoryToSaveMessagesTo,
                                       Messages = messages,
                                       Success = true,
                                       Exception = default(Exception),
                                   };
                    })
                .ContinueWith(a =>
                    {
                        var result = a.Result;

                        foreach (var message in result.Messages)
                        {
                            File.WriteAllText(Path.Combine(result.Directory.FullName, GenerateFileName(message)),
                                              FormatMessage(message));
                        }

                        return NotificationEvent.Success("Successfully downloaded {0} messages into {1}",
                                                         result.Messages.Count,
                                                         result.Directory);
                    })
                .ContinueWith(a =>
                    {
                        if (a.Exception != null)
                        {
                            Messenger.Default.Send(NotificationEvent.Fail(a.Exception.ToString(),
                                                                          "Something went wrong while attempting to download the messages"));

                            return;
                        }

                        Messenger.Default.Send(a.Result);
                    }, Context.UiThread);
        }

        string GenerateFileName(Message message)
        {
            var fileName = message.Id.Replace("\\", "---");

            return string.Format("{0}.txt", fileName);
        }

        string FormatMessage(Message message)
        {
            return string.Format(@"ID: {0}
Headers:
{1}

Body:
{2}", message.Id,
                                 string.Join(Environment.NewLine,
                                             message.Headers.Select(h => string.Format("    {0}: {1}", h.Key, h.Value))),
                                 message.Body);
        }

        void DeleteMessages(List<Message> messages)
        {
            Task.Factory
                .StartNew(() =>
                    {
                        var result = new
                            {
                                Deleted = new List<Message>(),
                                Failed = new List<Tuple<Message, string>>(),
                            };

                        foreach (var messageToDelete in messages)
                        {
                            try
                            {
                                DeleteMessage(messageToDelete);
                                result.Deleted.Add(messageToDelete);
                            }
                            catch (Exception e)
                            {
                                result.Failed.Add(new Tuple<Message, string>(messageToDelete, e.ToString()));
                            }
                        }

                        return result;
                    })
                .ContinueWith(t =>
                    {
                        var result = t.Result;

                        if (result.Failed.Any())
                        {
                            var details = string.Join(Environment.NewLine,
                                                      result.Failed.Select(
                                                          f => string.Format("Id {0}: {1}", f.Item1.Id, f.Item2)));

                            return NotificationEvent.Fail(details, "{0} messages deleted - {1} delete operations failed",
                                                          result.Deleted.Count, result.Failed.Count);
                        }

                        return NotificationEvent.Success("{0} messages moved", result.Deleted.Count);
                    })
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);
        }

        void MoveMessagesToSourceQueues(IEnumerable<Message> messagesToMove)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var canBeMoved = messagesToMove
                                      .Where(m => m.Headers.ContainsKey(Headers.SourceQueue));

                                  var result = new
                                                   {
                                                       Moved = new List<Message>(),
                                                       Failed = new List<Tuple<Message, string>>(),
                                                   };

                                  foreach (var message in canBeMoved)
                                  {
                                      try
                                      {
                                          MoveMessageToSourceQueue(message);
                                          result.Moved.Add(message);
                                      }
                                      catch (Exception e)
                                      {
                                          result.Failed.Add(new Tuple<Message, string>(message, e.ToString()));
                                      }
                                  }

                                  return result;
                              })
                .ContinueWith(t =>
                                  {
                                      var result = t.Result;

                                      if (result.Failed.Any())
                                      {
                                          var details = string.Join(Environment.NewLine, result.Failed.Select(f => string.Format("Id {0}: {1}", f.Item1.Id, f.Item2)));

                                          return NotificationEvent.Fail(details, "{0} messages moved - {1} move operations failed", result.Moved.Count, result.Failed.Count);
                                      }

                                      return NotificationEvent.Success("{0} messages moved", result.Moved.Count);
                                  })
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);
        }

        void MoveMessagesToQueue(List<Message> messagesToMove, bool shouldMoveMessages)
        {
            Task.Factory
                .StartNew(() => messagesToMove)
                .ContinueWith(t =>
                    {
                        var promptMessage =
                            string.Format("{0} {1} message(s) - please enter destination queue (e.g. 'someQueue@someMachine'): ",
                                shouldMoveMessages ? "Moving" : "Copying", messagesToMove.Count);

                        var destinationQueue = Prompt(promptMessage);

                        return new
                                   {
                                       DestinationQueue = destinationQueue,
                                       Messages = t.Result
                                   };
                    }, Context.UiThread)
                .ContinueWith(t =>
                    {
                        var result = new
                                         {
                                             Moved = new List<Message>(),
                                             Failed = new List<Tuple<Message, string>>(),
                                             DestinationQueue = t.Result.DestinationQueue,
                                         };

                        if (string.IsNullOrEmpty(t.Result.DestinationQueue))
                        {
                            result.Failed.AddRange(t.Result.Messages.Select(m => Tuple.Create(m, "No destination queue entered")));
                            return result;
                        }

                        foreach (var message in t.Result.Messages)
                        {
                            try
                            {
                                var leaveCopyInSourceQueue = !shouldMoveMessages;
                                MoveMessage(message, t.Result.DestinationQueue, leaveCopyInSourceQueue);
                                result.Moved.Add(message);
                            }
                            catch (Exception e)
                            {
                                result.Failed.Add(new Tuple<Message, string>(message, e.ToString()));
                            }
                        }

                        return result;
                    })
                .ContinueWith(t =>
                    {
                        var result = t.Result;

                        if (result.Failed.Any())
                        {
                            var details = string.Join(Environment.NewLine,
                                                      result.Failed.Select(
                                                          f => string.Format("Id {0}: {1}", f.Item1.Id, f.Item2)));

                            return NotificationEvent.Fail(details,
                                                          "{0} messages moved to {1} - {2} move operations failed",
                                                          result.Moved.Count, result.DestinationQueue,
                                                          result.Failed.Count);
                        }

                        return NotificationEvent.Success("{0} messages moved to {1}", result.Moved.Count,
                                                         result.DestinationQueue);
                    })
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);

        }

        string Prompt(string text)
        {
            var dialog = new PromptDialog { PromptText = text };

            dialog.ShowDialog();

            return dialog.ResultText;
        }

        void DeleteMessage(Message message)
        {
            using (var queue = new MessageQueue(message.QueuePath))
            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                try
                {
                    queue.ReceiveById(message.Id, transaction);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                    throw;
                }
            }

            Messenger.Default.Send(new MessageDeleted(message));
        }

        void MoveMessageToSourceQueue(Message message)
        {
            MoveMessage(message, message.Headers[Headers.SourceQueue], false);
        }

        static void MoveMessage(Message message, string destinationQueueName, bool leaveCopyInSourceQueue)
        {
            var sourceQueuePath = message.QueuePath;
            var destinationQueuePath = MsmqUtil.GetFullPath(destinationQueueName);

            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                try
                {
                    var sourceQueue = new MessageQueue(sourceQueuePath) { MessageReadPropertyFilter = DefaultFilter() };
                    var destinationQueue = new MessageQueue(destinationQueuePath);

                    var msmqMessage = sourceQueue.ReceiveById(message.Id, transaction);
                    destinationQueue.Send(msmqMessage, transaction);

                    if (leaveCopyInSourceQueue)
                    {
                        sourceQueue.Send(msmqMessage, transaction);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                    throw;
                }
            }

            Messenger.Default.Send(new MessageMoved(message, sourceQueuePath, destinationQueuePath, leaveCopyInSourceQueue));
        }

        void LoadMessages(Queue queue)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var messageQueue = new MessageQueue(queue.QueuePath);
                                  messageQueue.MessageReadPropertyFilter = DefaultFilter();

                                  var list = new List<Message>();

                                  using (var enumerator = messageQueue.GetMessageEnumerator2())
                                  {
                                      while (enumerator.MoveNext())
                                      {
                                          var message = enumerator.Current;
                                          var messageViewModel = GenerateMessage(message, queue.QueuePath);

                                          messageViewModel.ResetDirtyFlags();

                                          list.Add(messageViewModel);
                                      }
                                  }

                                  return new { Messages = list };
                              })
                .ContinueWith(t =>
                                  {
                                      if (t.Exception == null)
                                      {
                                          var result = t.Result;

                                          queue.SetMessages(result.Messages);

                                          return NotificationEvent.Success("{0} messages loaded from {1}",
                                                                           result.Messages.Count,
                                                                           queue.QueueName);
                                      }

                                      var details = t.Exception.ToString();
                                      return NotificationEvent.Fail(details, "Could not load messages from {0}: {1}",
                                                                    queue.QueueName,
                                                                    t.Exception);
                                  }, Context.UiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);
        }

        static MessagePropertyFilter DefaultFilter()
        {
            return new MessagePropertyFilter
                       {
                           Label = true,
                           ArrivedTime = true,
                           Extension = true,
                           Body = true,
                           Id = true,
                       };
        }

        static MessagePropertyFilter LosslessFilter()
        {
            return new MessagePropertyFilter
                       {
                           Label = true,
                           ArrivedTime = true,
                           Extension = true,
                           Body = true,
                           Id = true,

                           UseDeadLetterQueue = true,
                           UseJournalQueue = true,
                           TimeToBeReceived = true,
                       };
        }

        Message GenerateMessage(System.Messaging.Message message, string queuePath)
        {
            try
            {
                Dictionary<string, string> headers;
                var couldDeserializeHeaders = TryDeserializeHeaders(message, out headers);

                string body;
                int bodySize;
                var couldDecodeBody = TryDecodeBody(message, headers, out body, out bodySize);

                return new Message
                           {
                               Label = message.Label,
                               Time = message.ArrivedTime,
                               Headers = couldDeserializeHeaders ? new EditableDictionary<string, string>(headers) : new EditableDictionary<string, string>(),
                               Bytes = bodySize,
                               Body = body,
                               Id = message.Id,
                               QueuePath = queuePath,

                               CouldDeserializeBody = couldDecodeBody,
                               CouldDeserializeHeaders = couldDeserializeHeaders,
                           };
            }
            catch (Exception e)
            {
                return new Message
                           {
                               Body = string.Format(@"Message could not be properly decoded: 

{0}", e)
                           };
            }
        }

        void EncodeBody(System.Messaging.Message message, Message messageModel)
        {
            var headers = messageModel.Headers;
            var body = messageModel.Body;

            if (headers.ContainsKey(Headers.Encoding))
            {
                var encoding = headers[Headers.Encoding];
                var encoder = Encoding.GetEncoding(encoding);

                message.BodyStream = new MemoryStream(encoder.GetBytes(body));
            }
        }

        bool TryDecodeBody(System.Messaging.Message message, Dictionary<string, string> headers, out string body, out int bodySize)
        {
            try
            {
                if (headers == null)
                {
                    body = "Message has no headers that can be understood by Rebus";
                    bodySize = GetLengthFromStreamIfPossible(message);
                    return false;
                }

                if (!headers.ContainsKey(Headers.Encoding))
                {
                    body = string.Format("Message headers don't contain an element with the '{0}' key", Headers.Encoding);
                    bodySize = GetLengthFromStreamIfPossible(message);
                    return false;
                }

                var encoding = headers[Headers.Encoding];
                var encoder = Encoding.GetEncoding(encoding);

                using (var reader = new BinaryReader(message.BodyStream))
                {
                    var bytes = reader.ReadBytes((int)message.BodyStream.Length);
                    var str = encoder.GetString(bytes);
                    body = str;
                    bodySize = bytes.Length;
                    return true;
                }
            }
            catch (Exception e)
            {
                body = string.Format("An error occurred while decoding the body: {0}", e);
                bodySize = GetLengthFromStreamIfPossible(message);
                return false;
            }
        }

        static int GetLengthFromStreamIfPossible(System.Messaging.Message message)
        {
            try
            {
                return (int) message.BodyStream.Length;
            }
            catch
            {
                return -1;
            }
        }

        bool TryDeserializeHeaders(System.Messaging.Message message, out Dictionary<string, string> dictionary)
        {
            try
            {
                var headersAsJsonString = Encoding.UTF7.GetString(message.Extension);
                var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersAsJsonString);
                dictionary = headers;
                return true;
            }
            catch
            {
                dictionary = null;
                return false;
            }
        }

        void LoadQueues(Machine machine)
        {
            Task.Factory
                .StartNew(() =>
                    {
                        var queues = MessageQueue
                            .GetPrivateQueuesByMachine(machine.MachineName)
                            .Concat(new[]
                                        {
                                            // don't add non-transactional dead letter queue - wouldn't be safe!
                                            //new MessageQueue(string.Format(@"FormatName:DIRECT=OS:{0}\SYSTEM$;DeadLetter", machine.MachineName)),

                                            new MessageQueue(string.Format(@"FormatName:DIRECT=OS:{0}\SYSTEM$;DeadXact", machine.MachineName)),
                                        })
                            .ToArray();

                        return queues;
                    })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
                                      {
                                          try
                                          {
                                              var queues = t.Result
                                                            .Select(queue =>
                                                                {
                                                                    try
                                                                    {
                                                                        return new Queue(queue);
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        throw new ApplicationException(string.Format("An error occurred while loading message queue {0}", queue.Path), e);
                                                                    }
                                                                });

                                              machine.SetQueues(queues);

                                              return NotificationEvent.Success("{0} queues loaded from {1}",
                                                                               t.Result.Length,
                                                                               machine.MachineName);
                                          }
                                          catch (Exception e)
                                          {
                                              return NotificationEvent.Fail(e.ToString(), "Could not load queues from {0}: {1}",
                                                                           machine.MachineName, e.Message);
                                          }
                                      }

                                      return NotificationEvent.Fail(t.Exception.ToString(), "Could not load queues from {0}: {1}",
                                                                   machine.MachineName, t.Exception.Message);
                                  }, Context.UiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);
        }
    }
}