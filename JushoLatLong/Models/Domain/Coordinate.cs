namespace JushoLatLong.Model.Domain
{
    class Coordinate {
        public bool IsFound { get; set; } = false;
        public string Latitude { get; set; } = "0.0";
        public string Longitude { get; set; } = "0.0";
    }
}
