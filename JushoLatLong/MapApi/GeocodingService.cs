using JushoLatLong.Model.Api;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.MapApi
{
    public class GeocodingService
    {
        private const string _baseUrl = "https://maps.googleapis.com/maps/api/geocode/json?";
        private RestClient _restClient = null;
        private RestRequest _restRequest = null;

        // singleton
        private static GeocodingService instance = null;
        public static GeocodingService GetInstance(string mapApiKey)
        {
            if (instance == null) instance = new GeocodingService(mapApiKey);
            return instance;
        }

        GeocodingService(string mapApiKey)
        {
            _restClient = new RestClient(_baseUrl);
            _restRequest = new RestRequest("address={address}" + $"&key={mapApiKey}", Method.POST);
        }

        public GeoPoint GetGeoPoint(string address)
        {
            _restRequest.AddUrlSegment("address", address);

            var response = _restClient.Execute<GeocodingResponse>(_restRequest);
            var data = response.Data;

            if(data.Status == "OK")
            {
                //var geometry = data.Results.A
            }

            return null;
        }

    }
}
