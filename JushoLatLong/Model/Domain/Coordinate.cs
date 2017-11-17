using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Model.Domain {
    class Coordinate {
        public bool IsFound { get; set; } = false;
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
