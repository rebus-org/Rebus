using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            Messenger.Default.Register(this, (DeleteMessagesRequested request) => DeleteMessages(request.MessagesToMove));
            Messenger.Default.Register(this, (DownloadMessagesRequested request) => DownloadMessages(request.MessagesToDownload));
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
                                          MoveMessage(message);
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

        void MoveMessage(Message message)
        {
            var sourceQueuePath = message.QueuePath;
            var destinationQueuePath = MsmqUtil.GetFullPath(message.Headers[Headers.SourceQueue]);

            using (var transaction = new MessageQueueTransaction())
            {
                transaction.Begin();
                try
                {
                    var sourceQueue = new MessageQueue(sourceQueuePath) { MessageReadPropertyFilter = DefaultFilter() };
                    var destinationQueue = new MessageQueue(destinationQueuePath);

                    var msmqMessage = sourceQueue.ReceiveById(message.Id, transaction);
                    destinationQueue.Send(msmqMessage, transaction);

                    transaction.Commit();
                }
                catch
                {
                    transaction.Abort();
                    throw;
                }
            }

            Messenger.Default.Send(new MessageMoved(message, sourceQueuePath, destinationQueuePath));
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
                                          list.Add(GenerateMessage(message, queue.QueuePath));
                                      }
                                  }

                                  return new { Messages = list };
                              })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
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

        Message GenerateMessage(System.Messaging.Message message, string queuePath)
        {
            try
            {
                var headers = TryDeserializeHeaders(message);

                return new Message
                           {
                               Label = message.Label,
                               Time = message.ArrivedTime,
                               Headers = headers,
                               Bytes = TryDetermineMessageSize(message),
                               Body = TryDecodeBody(message, headers),
                               Id = message.Id,
                               QueuePath = queuePath,
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

        int TryDetermineMessageSize(System.Messaging.Message message)
        {
            try
            {
                return (int)message.BodyStream.Length;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        string TryDecodeBody(System.Messaging.Message message, Dictionary<string, string> headers)
        {
            if (headers.ContainsKey(Headers.Encoding))
            {
                var encoding = headers[Headers.Encoding];
                var encoder = Encoding.GetEncoding(encoding);

                using (var reader = new BinaryReader(message.BodyStream))
                {
                    var bytes = reader.ReadBytes((int)message.BodyStream.Length);
                    var str = encoder.GetString(bytes);
                    return str;
                }
            }

            return "(message encoding not specified)";
        }

        Dictionary<string, string> TryDeserializeHeaders(System.Messaging.Message message)
        {
            try
            {
                var headersAsJsonString = Encoding.UTF7.GetString(message.Extension);
                var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersAsJsonString);
                return headers ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        void LoadQueues(Machine machine)
        {
            Task.Factory
                .StartNew(() =>
                              {
                                  var privateQueues = MessageQueue.GetPrivateQueuesByMachine(machine.MachineName);

                                  return privateQueues;
                              })
                .ContinueWith(t =>
                                  {
                                      if (!t.IsFaulted)
                                      {
                                          var queues = t.Result
                                              .Select(queue => new Queue(queue));

                                          machine.SetQueues(queues);

                                          return NotificationEvent.Success("{0} queues loaded from {1}",
                                                                       t.Result.Length,
                                                                       machine.MachineName);
                                      }

                                      var details = t.Exception.ToString();
                                      return NotificationEvent.Fail(details, "Could not load queues from {0}: {1}",
                                                                   machine.MachineName, t.Exception.Message);
                                  }, Context.UiThread)
                .ContinueWith(t => Messenger.Default.Send(t.Result), Context.UiThread);
        }
    }
}