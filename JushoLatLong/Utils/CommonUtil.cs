using GoogleMaps.LocationServices;
using JushoLatLong.ViewModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Utils
{
    public class CommonUtil
    {
        public List<string> GetPropertyValues(dynamic expando)
        {
            List<string> toReturn = new List<string>();

            JObject attributesAsJObject = expando;
            Dictionary<string, object> attrDictionary = attributesAsJObject.ToObject<Dictionary<string, object>>();

            foreach (string val in attrDictionary.Values)
            {
                toReturn.Add(val);
            }

            return toReturn;
        }

        public List<string> GetPropertyKeys(dynamic expando)
        {
            List<string> toReturn = new List<string>();

            JObject attributesAsJObject = expando;
            Dictionary<string, object> attrDictionary = attributesAsJObject.ToObject<Dictionary<string, object>>();

            foreach (string key in attrDictionary.Keys)
            {
                toReturn.Add(key);
            }

            return toReturn;
        }

        public bool ValidateApiKey(MainWindow mainWindow, ActivityViewModel viewModel)
        {

            // check: map api key is provided
            if (String.IsNullOrEmpty(viewModel.MapApiKey))
            {
                mainWindow.ShowMessage("Please provide Google Map API Key");
                return false;
            }

            // check: map api key is valid & did not exceed query limit
            try
            {
                var point = new GoogleLocationService(viewModel.MapApiKey).GetLatLongFromAddress("1 Chome-9 Marunouchi, Chiyoda-ku, Tōkyō-to 100-0005");
            }
            catch (WebException ex)
            {
                mainWindow.ShowErrorMessage("Invalid API Key or query limit exceeded!");
                return false;
            }

            return true;
        }
    }
}
