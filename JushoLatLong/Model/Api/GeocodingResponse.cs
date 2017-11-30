using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Model.Api
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    public class GeocodingResponse
    {
        public List<Result> Results { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Result
    {
        public List<AddressComponent> AddressComponents { get; set; }
        public string FormattedAddress { get; set; }
        public Geometry Geometry { get; set; }
        public string PlaceId { get; set; }
        public List<string> Types { get; set; }
    }

    public class Geometry
    {
        public Location Location { get; set; }
        public string LocationType { get; set; }
        public Viewport Viewport { get; set; }
    }

    public class Viewport
    {
        public Location Northeast { get; set; }
        public Location Southwest { get; set; }
    }

    public class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class AddressComponent
    {
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public List<string> Types { get; set; }
    }
}
