using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Model.Domain {

    class CompanyProfile {

        public string CompanyCode { get; set; }
        public string PrefecturesName { get; set; }
        public string CityName { get; set; }
        public string Address { get; set; }
        public string Remarks { get; set; }
        public string Registrant { get; set; }
        public string RegistrationDate { get; set; }
        public string Apo { get; set; }
        public string RecordNumber { get; set; }

        // added fields
        public string Latitude { get; set; } = "0.0";
        public string Longitude { get; set; } = "0.0";
    }
}
