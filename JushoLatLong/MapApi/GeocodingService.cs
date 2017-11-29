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
        private RestRequest _request = null;

        public GeocodingService(string apiKey)
        {
            _restClient = new RestClient(BaseUri)
            {
                Encoding = Encoding.UTF8,
            };

            _request = new RestRequest("json?address={address}&key=" + $"{apiKey}")
            {
                Method = Method.GET,
                RequestFormat = DataFormat.Json,
                OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; }
            };
        }

        public async Task<GeoPoint> GetGeoPointAsync(string address)
        {

            _request.AddOrUpdateParameter("address", address, ParameterType.UrlSegment);

            var response = await _restClient.ExecuteGetTaskAsync<GeocodingResponse>(_request);

            if (response != null && response.Data != null && response.Data.Results.Count > 0)
            {
                var result = response.Data.Results.ElementAt(0);
                return new GeoPoint { Latitude = result.Geometry.Location.Lat, Longitude = result.Geometry.Location.Lng };
            }

            return new GeoPoint();
        }

        public GeoPoint GetGeoPoint(string address)
        {

            _request.AddOrUpdateParameter("address", address, ParameterType.UrlSegment);

            var response = _restClient.Execute<GeocodingResponse>(_request);

            if (response != null && response.Data != null && response.Data.Results.Count > 0)
            {
                var result = response.Data.Results.ElementAt(0);
                return new GeoPoint { Latitude = result.Geometry.Location.Lat, Longitude = result.Geometry.Location.Lng };
            }

            return new GeoPoint();
        }
    }
}
