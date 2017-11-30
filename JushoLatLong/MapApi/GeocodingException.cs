using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.MapApi
{
    public class GeocodingException: Exception
    {
        public GeocodingException() : base()
        {

        }

        public GeocodingException(string msg): base(msg)
        {

        }

        public GeocodingException(string msg, Exception inner) : base(msg, inner)
        {

        }
    }
}
