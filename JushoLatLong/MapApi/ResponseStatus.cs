using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.MapApi
{
    public class ResponseStatus
    {
        public const string Ok = "OK";
        public const string ZeroResults = "ZERO_RESULTS";
        public const string OverQueryLimit = "OVER_QUERY_LIMIT";
        public const string RequestDenied = "REQUEST_DENIED";
        public const string InvalidRequest = "INVALID_REQUEST";
        public const string UnknownError = "UNKNOWN_ERROR";
    }
}
