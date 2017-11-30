using JushoLatLong.Model.Api;
using JushoLatLong.Utils;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static GoogleMaps.LocationServices.Constants;

namespace JushoLatLong.MapApi
{

    public class GeocodingService
    {
        private RestClient _restClient = null;
        private RestRequest _request = null;

        public GeocodingService(string apiKey)
        {
            _restClient = new RestClient(Constants.BaseUri)
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

            if (response != null && response.Data != null)
            {
                var status = response.Data.Status;

                if (status == ResponseStatus.Ok || response.Data.Results.Count > 0)
                {
                    // "OK" indicates that no errors occurred; the address was successfully parsed and at least one geocode was returned.
                    var result = response.Data.Results.ElementAt(0);
                    return new GeoPoint { Latitude = result.Geometry.Location.Lat, Longitude = result.Geometry.Location.Lng };
                }
                else if(status == ResponseStatus.OverQueryLimit)
                {
                    // "OVER_QUERY_LIMIT" indicates that you are over your quota.
                    throw new GeocodingException("Query Limit Exceeded");
                }else if (status == ResponseStatus.ZeroResults || status == ResponseStatus.InvalidRequest || status == ResponseStatus.UnknownError)
                {
                    // "ZERO_RESULTS" indicates that the geocode was successful but returned no results. This may occur if the geocoder was passed a non-existent address.
                    // "INVALID_REQUEST" generally indicates that the query (address, components or latlng) is missing.
                    // "UNKNOWN_ERROR" indicates that the request could not be processed due to a server error. The request may succeed if you try again.
                    return new GeoPoint();
                }
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

        public static bool IsApiKeyValid(string apiKey)
        {
            var testAddress = "1 Chome-9 Marunouchi, Chiyoda-ku, Tōkyō-to 100-0005";

            var restClient = new RestClient(Constants.BaseUri)
            {
                Encoding = Encoding.UTF8,
            };

            var request = new RestRequest($"json?address={testAddress}&key={apiKey}")
            {
                Method = Method.GET,
                RequestFormat = DataFormat.Json,
                OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; }
            };

            var response = restClient.Execute<GeocodingResponse>(request);

            if (response?.Data.ErrorMessage == Constants.InvaliKeyErrorMessage || response?.Data.Status== ResponseStatus.RequestDenied)
            {
                // InvaliKeyErrorMessage = "The provided API key is invalid."
                // "REQUEST_DENIED" indicates that your request was denied.
                return false;
            }

            return true;
        }

    }
}
