using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Google.Apis.Gmail.v1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailForwarder
{
    public class Data
    {
        public string yourAddress { get; set; }
        public string addressToForward { get; set; }
        public string filter { get; set; }
        public string labelName { get; set; }
        public int filterType { get; set; }

        public static ObservableCollection<Data> LoadData(string yourAddress, string fileDestination, string savedDataLocation, GmailService service)
        {
            ObservableCollection<Data> result = new ObservableCollection<Data>();

            if (File.Exists(savedDataLocation))
            {
                string[] lines = File.ReadAllLines(savedDataLocation);

                foreach (var line in lines)
                {
                    int pFromYA = 0;
                    int pToYA = line.IndexOf(";");
                    var yourAddressLoad = line.Substring(pFromYA, pToYA - pFromYA);

                    if (yourAddress == yourAddressLoad)
                    {
                        int pFromATF = pToYA + ";".Length;
                        int pToATF = line.IndexOf(";", pFromATF);
                        var addressToForwardLoad = line.Substring(pFromATF, pToATF - pFromATF);

                        int pFromAFF = pToATF + ";".Length;
                        int pToAFF = line.IndexOf(";", pFromAFF);
                        var addressFromForwardLoad = line.Substring(pFromAFF, pToAFF - pFromAFF);

                        int pFromLN = pToAFF + ";".Length;
                        int pToLN = line.IndexOf(";", pFromLN);
                        var labelNameLoad = line.Substring(pFromLN, pToLN - pFromLN);

                        int pFromFT = pToLN + ";".Length;
                        int pToFT = line.Length;
                        var filterTypeLoad = int.Parse(line.Substring(pFromFT, pToFT - pFromFT));

                        result.Add(new Data { filter = addressFromForwardLoad, addressToForward = addressToForwardLoad, filterType = filterTypeLoad, labelName = labelNameLoad, yourAddress = yourAddressLoad });

                        SendingFactory.NewForward(addressToForwardLoad, addressFromForwardLoad, labelNameLoad, fileDestination, filterTypeLoad, service, yourAddress);
                    }
                }
            }
            return result;
        }
    }
}
