using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System.IO;

namespace EmailForwarder
{
    public class Sending
    {
        private readonly string AddressToForward;
        private readonly string LabelName;
        private readonly string FileDestination;
        private readonly GmailService Service;


        public Sending(string addressToForward, string labelName, string fileDestination, GmailService service)
        {
            AddressToForward = addressToForward;
            LabelName = labelName;
            Service = service;
            FileDestination = fileDestination;
        }

        public Task MessageCheck(List<string> labelIDtoSend, int sendOrReply)
        {
            var messagesList = Utilities.MessagesList(Service, labelIDtoSend.ElementAt(0));
            if (messagesList != null)
            {
                if (sendOrReply == 1)
                    MessageSend(messagesList);

                else if (sendOrReply == 2)
                    SendReply(messagesList);
            }
            return null;
        }

        public void MessageSend(List<Message> messagesList)
        {
            string addressToReplace = "";

            foreach (var message in messagesList)
            {
                var newMsg = new Message();
                var messageID = message.Id;

                var messageText = Service.Users.Messages.Get("me", messageID);
                var getMessage = messageText.Execute();
                string address = "";

                foreach (var header in getMessage.Payload.Headers)
                {
                    if (header.Name == "From")
                    {
                        string fromAddress = header.Value;
                        if (fromAddress.Contains("<"))
                        {
                            int pFrom = fromAddress.IndexOf("<") + "<".Length;
                            int pTo = fromAddress.LastIndexOf(">");
                            address = fromAddress.Substring(pFrom, pTo - pFrom);
                        }
                        else
                            address = header.Value;

                        Utilities.SaveThreadID(FileDestination, address, getMessage.ThreadId);
                    }

                    else if (header.Name == "To")
                    {
                        addressToReplace = header.Value;
                    }
                }

                messageText.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                getMessage = messageText.Execute();


                newMsg.ThreadId = getMessage.ThreadId;
                //newMsg.LabelIds = labelID;

                var testowe = getMessage.Raw.Replace('-', '+').Replace('_', '/');
                byte[] data = Convert.FromBase64String(testowe);
                string decoded = Encoding.UTF8.GetString(data);
                decoded = decoded.Replace(Environment.NewLine + "To: " + addressToReplace + Environment.NewLine,
                                          Environment.NewLine + "To: " + AddressToForward + Environment.NewLine);

                newMsg.Raw = ToBase64UrlEncode(decoded);

                try
                {

                    Service.Users.Messages.Send(newMsg, "me").Execute();
                    Utilities.ChangeLabels(Service, messageID, LabelName + "/To_Forward", LabelName);

                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            }

        }

        public void SendReply(List<Message> messagesList)
        {
            string addressToReplace = "";
            foreach (var message in messagesList)
            {
                int retryCount = 3;
                int currentRetry = 0;

                var newMsg = new Message();
                string addressToReply = "";
                var messageID = message.Id;
                var messageText = Service.Users.Messages.Get("me", messageID);
                var getMessage = messageText.Execute();

                foreach (var header in getMessage.Payload.Headers)
                {
                    if (header.Name == "To")
                    {
                        addressToReplace = header.Value;
                        break;
                    }
                }

                messageText.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                getMessage = messageText.Execute();
                string[] lines = null;

                while (true)
                {
                    try
                    {
                        lines = File.ReadAllLines(FileDestination);
                        break;
                    }
                    catch
                    {
                        currentRetry++;

                        if (currentRetry > retryCount)
                            return;
                    }
                }


                foreach (var line in lines)
                {
                    if (line.Contains(getMessage.ThreadId))
                    {
                        int pFrom = line.IndexOf(";") + ";".Length;
                        int pTo = line.IndexOf(";", pFrom);
                        addressToReply = line.Substring(pFrom, pTo - pFrom);
                        break;
                    }
                    //else
                    //{
                    //    //What if someone change the subject while replying, causing the thread ID to be changed too...
                    //}
                }

                var testowe = getMessage.Raw.Replace('-', '+').Replace('_', '/');
                byte[] data = Convert.FromBase64String(testowe);
                string decoded = Encoding.UTF8.GetString(data);
                decoded = decoded.Replace(Environment.NewLine + "To: " + addressToReplace + Environment.NewLine,
                                          Environment.NewLine + "To: " + addressToReply + Environment.NewLine);

                newMsg.ThreadId = getMessage.ThreadId;

                newMsg.Raw = ToBase64UrlEncode(decoded);

                try
                {
                    Service.Users.Messages.Send(newMsg, "me").Execute();
                    Utilities.ChangeLabels(Service, messageID, LabelName + "/To_Reply", LabelName);

                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            }
        }



        private static string ToBase64UrlEncode(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(inputBytes)
              .Replace('+', '-')
              .Replace('/', '_')
              .Replace("=", "");
        }


    }
}

