using JushoLatLong.Model.Api;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JushoLatLong.MapApi
{
    public class GeocodingService
    {
        private const string BaseUri = "https://maps.googleapis.com/maps/api/geocode/";

        private RestClient _restClient = null;

        private RestRequest Request { get; set; }

        public GeocodingService(string apiKey)
        {
            _restClient = new RestClient(BaseUri)
            {
                Encoding = Encoding.UTF8,
            };

            Request = new RestRequest("json?address={address}&key=" + $"{apiKey}", Method.GET);
        }

        public async Task<GeoPoint> GetGeoPointAsync(string address)
        {
            
            Request.AddUrlSegment("address", address);

            var response = await _restClient.ExecuteGetTaskAsync<GeocodingResponse>(Request);

            if (response.IsSuccessful && response.Data != null && response.Data.Results.Count > 0)
            {
                var result = response.Data.Results.ElementAt(0);
                return new GeoPoint { Latitude = result.Geometry.Location.Lat, Longitude = result.Geometry.Location.Lng };
            }

            return new GeoPoint();
        }
    }
}
