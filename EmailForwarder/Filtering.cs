using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Collections.Generic;

namespace EmailForwarder
{
    public class Filtering
    {
        public static Label CreateLabel(GmailService service, String newLabelName)
        {
            Label label = new Label();
            label.Name = newLabelName;

            try
            {
                return service.Users.Labels.Create(label, "me").Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
            return null;
        }

        public static List<string> GetLabelId(GmailService service, string LabelName)
        {
            var request = service.Users.Labels.List("me");
            IList<Label> labels = request.Execute().Labels;

            List<string> labelIds = new List<string>();

            foreach (var labelItem in labels)
            {
                if (labelItem.Name == LabelName)
                {
                    var labelID = labelItem.Id;
                    labelIds.Add(labelID);
                    break;
                }
            }
            return labelIds;
        }

        public static Filter FilterAdd(GmailService service, List<string> labelsIds, string Filter, int filterType, string yourAddress, string addressToForward) //filterType = 1 -> from; filterType = 2 -> Subject; 
        {
            Filter test1 = new Filter();
            FilterCriteria criteria1 = new FilterCriteria();
            FilterAction action1 = new FilterAction();
            var inbox = new List<string> { "INBOX" };

            switch (filterType)
            {
                case 1:
                    criteria1.From = Filter;
                    break;
                case 2:
                    criteria1.Subject = Filter;
                    criteria1.From = "-" + yourAddress + ", -" + addressToForward;
                    break;
                case 3:
                    criteria1.From = addressToForward;
                    criteria1.Subject = Filter;
                    break;
                default:
                    return null;
            }
            action1.AddLabelIds = labelsIds;
            action1.RemoveLabelIds = inbox;

            test1.Criteria = criteria1;
            test1.Action = action1;

            try
            {
                return service.Users.Settings.Filters.Create(test1, "me").Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
            return null;
        }

        public static void FilterDelete(GmailService service, string filter, string toSendAddress, string labelName)
        {
            var labelID = GetLabelId(service, labelName+ "/To_Forward");
            var labelID2 = GetLabelId(service, labelName + "/To_Reply");
            var request = service.Users.Settings.Filters.List("me");
            IList<Filter> filters = request.Execute().Filter;

            try
            {
                foreach (var filterItem in filters)
                {
                    if ((filterItem.Criteria.From == filter || filterItem.Criteria.Subject == filter) && filterItem.Action.AddLabelIds[0] == labelID[0])
                    {
                        var filterID = filterItem.Id;
                        service.Users.Settings.Filters.Delete("me", filterID).Execute();
                    }
                    else if (filterItem.Criteria.From == toSendAddress && filterItem.Action.AddLabelIds[0] == labelID2[0])
                    {
                        var filterID = filterItem.Id;
                        service.Users.Settings.Filters.Delete("me", filterID).Execute();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
