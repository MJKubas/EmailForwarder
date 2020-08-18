using Google.Apis.Gmail.v1;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace EmailForwarder
{
    public class SendingFactory
    {
        public static void NewForward(string addressToForward, string filter, string labelName, string fileDestination, int filterType, GmailService service, string yourAddress)
        {
            Filtering.CreateLabel(service, labelName);
            Filtering.CreateLabel(service, labelName + "/To_Forward");
            Filtering.CreateLabel(service, labelName + "/To_Reply");

            var labelID = Filtering.GetLabelId(service, labelName + "/To_Forward");
            var label2ID = Filtering.GetLabelId(service, labelName + "/To_Reply");

            if(filterType == 1)
            {
                Filtering.FilterAdd(service, labelID, filter, 1, yourAddress, addressToForward);
                Filtering.FilterAdd(service, label2ID, addressToForward, 1, yourAddress, addressToForward);
            }
            else if(filterType == 2)
            {
                Filtering.FilterAdd(service, labelID, filter, 2, yourAddress, addressToForward);
                Filtering.FilterAdd(service, label2ID, filter, 3, yourAddress, addressToForward);
            }


            Sending newSend = new Sending(addressToForward, labelName, fileDestination, service);

            Method(labelID, 1, newSend);
            Method(label2ID, 2, newSend);

        }

        private static async void Method(List<string> labelID, int sendOrReply, Sending newSend)
        {
            await RunSend(labelID, sendOrReply, newSend);
            Method(labelID, sendOrReply, newSend);
        }

        private static Task RunSend(List<string> labelID, int sendOrReply, Sending newSend)
        {
            return Task.Factory.StartNew(() =>
            {
                newSend.MessageCheck(labelID, sendOrReply);
                System.Threading.Thread.Sleep(30000);
            });
        }
    }
}
