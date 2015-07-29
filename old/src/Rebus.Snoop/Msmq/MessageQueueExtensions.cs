using System;
using System.Messaging;

namespace Rebus.Snoop.Msmq
{
    public static class MessageQueueExtensions
    {
        public static uint GetCountNaive(this MessageQueue queue)
        {
            return GetMessageCount(queue);
        }

        static Message PeekWithoutTimeout(MessageQueue q, Cursor cursor, PeekAction action)
        {
            Message ret = null;
            try
            {
                ret = q.Peek(new TimeSpan(1), cursor, action);
            }
            catch (MessageQueueException mqe)
            {
                if (!mqe.Message.ToLower().Contains("timeout"))
                {
                    throw;
                }
            }
            return ret;
        }

        static uint GetMessageCount(MessageQueue q)
        {
            uint count = 0;
            var cursor = q.CreateCursor();

            var m = PeekWithoutTimeout(q, cursor, PeekAction.Current);
            if (m != null)
            {
                count = 1;
                while ((PeekWithoutTimeout(q, cursor, PeekAction.Next)) != null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    static class MessageQueueExtensions2
    {
        public static uint GetCount(this MessageQueue queue)
        {
            var flags = new MessagePropertyFilter
                            {
                                AdministrationQueue = false,
                                ArrivedTime = false,
                                CorrelationId = false,
                                Priority = false,
                                ResponseQueue = false,
                                SentTime = false,
                                Body = false,
                                Label = false,
                                Id = false,
                                AcknowledgeType = false,
                                Acknowledgment = false,
                                AppSpecific = false,
                                AttachSenderId = false,
                                Authenticated = false,
                                AuthenticationProviderName = false,
                                AuthenticationProviderType = false,
                                ConnectorType = false,
                                DestinationQueue = false,
                                DestinationSymmetricKey = false,
                                DigitalSignature = false,
                                EncryptionAlgorithm = false,
                                Extension = false,
                                HashAlgorithm = false,
                                IsFirstInTransaction = false,
                                IsLastInTransaction = false,
                                LookupId = false,
                                MessageType = false,
                                Recoverable = false,
                                SenderCertificate = false,
                                SenderId = false,
                                SenderVersion = false,
                                SourceMachine = false,
                                TimeToBeReceived = false,
                                TimeToReachQueue = false,
                                TransactionId = false,
                                TransactionStatusQueue = false,
                                UseAuthentication = false,
                                UseDeadLetterQueue = false,
                                UseEncryption = false,
                                UseJournalQueue = false,
                                UseTracing = false
                            };

            var newQueue = new MessageQueue(queue.Path);
            newQueue.MessageReadPropertyFilter = flags;
            return (uint) newQueue.GetAllMessages()
                                  .Length;
            //return GetCount(queue.Path);
        }

        //[DllImport("mqrt.dll")]
        //private unsafe static extern int MQMgmtGetInfo(char* computerName, char* objectName, MQMGMTPROPS* mgmtProps);

        //private const byte VT_NULL = 1;
        //private const byte VT_UI4 = 19;
        //private const int PROPID_MGMT_QUEUE_MESSAGE_COUNT = 7;

        ////size must be 16
        //[StructLayout(LayoutKind.Sequential)]
        //private struct MQPROPVariant
        //{
        //    public byte vt;       //0
        //    public byte spacer;   //1
        //    public short spacer2; //2
        //    public int spacer3;   //4
        //    public uint ulVal;    //8
        //    public int spacer4;   //12
        //}

        ////size must be 16 in x86 and 28 in x64

        //[StructLayout(LayoutKind.Sequential)]
        //private unsafe struct MQMGMTPROPS
        //{
        //    public uint cProp;
        //    public int* aPropID;
        //    public MQPROPVariant* aPropVar;
        //    public int* status;
        //}

        //private static unsafe uint GetCount(string path)
        //{
        //    var props = new MQMGMTPROPS {cProp = 1};
        //    var aPropId = PROPID_MGMT_QUEUE_MESSAGE_COUNT;
        //    props.aPropID = &aPropId;

        //    var aPropVar = new MQPROPVariant {vt = VT_NULL};
        //    props.aPropVar = &aPropVar;

        //    var status = 0;
        //    props.status = &status;

        //    var objectName = Marshal.StringToBSTR("queue=Direct=OS:" + path);
            
        //    try
        //    {
        //        var result = MQMgmtGetInfo(null, (char*)objectName, &props);
        //        if (result != 0 || *props.status != 0 || props.aPropVar->vt != VT_UI4)
        //        {
        //            return 0;
        //        }
        //        else
        //        {
        //            return props.aPropVar->ulVal;
        //        }
        //    }
        //    finally
        //    {
        //        Marshal.FreeBSTR(objectName);
        //    }
        //}
    }
}