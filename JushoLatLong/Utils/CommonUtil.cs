using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
