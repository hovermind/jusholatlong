using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Utils
{
    public static class Constants
    {
        public const string BaseUri = "https://maps.googleapis.com/maps/api/geocode/";
        public const string InvaliKeyErrorMessage = "The provided API key is invalid."; // from GeoCoding Doc.

        public const string ApiRegionFromLatLong = "maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false";
        public const string ApiLatLongFromAddress = "maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false";
        public const string ApiDirections = "maps.googleapis.com/maps/api/directions/xml?origin={0}&destination={1}&sensor=false";

    }
}
