using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace JushoLatLong.Utils {
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
    }
}
