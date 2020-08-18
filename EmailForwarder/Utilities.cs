using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace EmailForwarder
{
    public class Utilities
    {
        public static void CheckNRun(ref ObservableCollection<Data> Data, string yourAddress, string addressToForward, string filter, string labelName, string savedDataLocation, string threadIDAddressLocation, int filterType, GmailService service)
        {
            Data toAdd = new Data() { yourAddress = yourAddress, addressToForward = addressToForward, filter = filter, filterType = filterType, labelName = labelName };

            bool exist = false;

            if (Data != null)
            {
                foreach (var line in Data)
                {
                    if (line == toAdd)
                    {
                        exist = true;
                    }
                }
            }

            if (exist == false)
            {

                SendingFactory.NewForward(addressToForward, filter, labelName, threadIDAddressLocation, filterType, service, yourAddress);

                Data.Add(toAdd);

                SaveToFile(ref Data, savedDataLocation);
            }
        }

        public static List<Message> MessagesList(GmailService service, string labelId)
        {
            List<Message> result = new List<Message>();
            var messageList = service.Users.Messages.List("me");
            messageList.LabelIds = labelId;
            do
            {
                try
                {
                    ListMessagesResponse response = messageList.Execute();
                    if (response.Messages != null)
                    {
                        result.AddRange(response.Messages);
                        messageList.PageToken = response.NextPageToken;
                    }
                    else
                        return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!string.IsNullOrEmpty(messageList.PageToken));

            result.Reverse();
            return result;
        }

        public static void SaveToFile(ref ObservableCollection<Data> Data, string savedDataLocation)
        {
            if (File.Exists(savedDataLocation))
            {
                File.Copy(savedDataLocation, savedDataLocation + ".bac", true);
                File.Delete(savedDataLocation);
            }
            foreach (var line in Data)
            {
                string toSave = line.yourAddress + ";" + line.addressToForward + ";" + line.filter + ";" + line.labelName + ";" + line.filterType + Environment.NewLine;
                File.AppendAllText(savedDataLocation, toSave);
            }
        }

        public static void FileClean(string fileDestination)
        {
            if(File.Exists(fileDestination))
            {
                string[] lines = File.ReadAllLines(fileDestination);
                List<string> Llines = new List<string>(lines);

                for (int i = Llines.Count - 1; i >= 0; i--)
                {
                    var firstSeparator = Llines[i].IndexOf(";") + ";".Length;
                    var secondSeparator = Llines[i].IndexOf(";", firstSeparator + ";".Length);
                    var date = Llines[i].Substring(secondSeparator + ";".Length, Llines[i].Length - (secondSeparator + ";".Length));
                    DateTime toCompare = DateTime.Parse(date);

                    if ((DateTime.Now - toCompare).TotalDays > 60)
                    {
                        Llines.Remove(Llines[i]);
                    }
                }
                File.WriteAllLines(fileDestination, Llines);
            }
        }

        public static void ChangeLabels(GmailService service, string messageID, string labelToRemove, string labelToAdd)
        {
            ModifyMessageRequest mods = new ModifyMessageRequest();
            mods.AddLabelIds = Filtering.GetLabelId(service, labelToAdd);
            mods.RemoveLabelIds = Filtering.GetLabelId(service, labelToRemove);
            try
            {
                service.Users.Messages.Modify(mods, "me", messageID).Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        public static void SaveThreadID(string fileDestination, string address, string threadID)
        {
            int retryCount = 3;
            int currentRetry = 0;
            string[] lines;

            if (File.Exists(fileDestination))
            {
                while (true)
                {
                    try
                    {
                        lines = File.ReadAllLines(fileDestination);
                        currentRetry = 0;
                        break;
                    }
                    catch
                    {
                        currentRetry++;

                        if (currentRetry > retryCount)
                            return;
                    }
                }

                int lineNumber = -1;

                for (int i = 0; i <= lines.Count() - 1; i++)
                {
                    if (lines[i].Contains(threadID))
                    {
                        lineNumber = i;
                    }
                }

                if (lineNumber == -1)
                {
                    while (true)
                    {
                        try
                        {
                            File.AppendAllText(fileDestination, threadID + ";" + address + ";" + DateTime.Now.ToString() + Environment.NewLine);
                            currentRetry = 0;
                            break;
                        }
                        catch
                        {
                            currentRetry++;

                            if (currentRetry > retryCount)
                                return;
                        }
                    }
                }

                else
                {
                    var firstSeparator = lines[lineNumber].IndexOf(";") + ";".Length;
                    var secondSeparator = lines[lineNumber].IndexOf(";", firstSeparator + ";".Length);
                    var date = lines[lineNumber].Substring(secondSeparator + ";".Length, lines[lineNumber].Length - (secondSeparator + ";".Length));
                    lines[lineNumber].Replace(date, DateTime.Now.ToString());

                    while (true)
                    {
                        try
                        {
                            File.WriteAllLines(fileDestination, lines);
                            currentRetry = 0;
                            break;
                        }
                        catch
                        {
                            currentRetry++;

                            if (currentRetry > retryCount)
                                return;
                        }
                    }
                }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        File.AppendAllText(fileDestination, threadID + ";" + address + ";" + DateTime.Now.ToString() + Environment.NewLine);
                        currentRetry = 0;
                        break;
                    }
                    catch
                    {
                        currentRetry++;

                        if (currentRetry > retryCount)
                            return;
                    }
                }
            }
        }
    }
}
