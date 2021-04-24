using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Amazon.SellingPartnerAPIAA
{
    public class LWAClient
    {
        public const string AccessTokenKey = "access_token";
        public const string AccessTokenExpireKey = "expires_in";
        public const string JsonMediaType = "application/json; charset=utf-8";

        public IRestClient RestClient { get; set; }
        public LWAAccessTokenRequestMetaBuilder LWAAccessTokenRequestMetaBuilder { get; set; }
        public LWAAuthorizationCredentials LWAAuthorizationCredentials { get; private set; }

        private string AccessToken = null;
        private DateTime AccessTokenValidTill = DateTime.MinValue;

        public LWAClient(LWAAuthorizationCredentials lwaAuthorizationCredentials)
        {
            LWAAuthorizationCredentials = lwaAuthorizationCredentials;
            LWAAccessTokenRequestMetaBuilder = new LWAAccessTokenRequestMetaBuilder();
            RestClient = new RestClient(LWAAuthorizationCredentials.Endpoint.GetLeftPart(System.UriPartial.Authority));
        }

        /// <summary>
        /// Retrieves access token from LWA
        /// </summary>
        /// <param name="lwaAccessTokenRequestMeta">LWA AccessTokenRequest metadata</param>
        /// <param name="force">Enforce LWA access token request also if still valid</param>
        /// <returns>LWA Access Token</returns>
        public virtual string GetAccessToken(bool force = false)
        {
            if (!force
                && !String.IsNullOrEmpty(AccessToken)
                && DateTime.UtcNow < AccessTokenValidTill)
                return AccessToken;

            LWAAccessTokenRequestMeta lwaAccessTokenRequestMeta = LWAAccessTokenRequestMetaBuilder.Build(LWAAuthorizationCredentials);
            var accessTokenRequest = new RestRequest(LWAAuthorizationCredentials.Endpoint.AbsolutePath, Method.POST);

            string jsonRequestBody = JsonConvert.SerializeObject(lwaAccessTokenRequestMeta);

            accessTokenRequest.AddParameter(JsonMediaType, jsonRequestBody, ParameterType.RequestBody);

            string accessToken;
            try
            {
                var response = RestClient.Execute(accessTokenRequest);

                if (!IsSuccessful(response))
                {
                    throw new IOException("Unsuccessful LWA token exchange", response.ErrorException);
                }

                JObject responseJson = JObject.Parse(response.Content);

                accessToken = responseJson.GetValue(AccessTokenKey).ToString();
                AccessToken = accessToken;
                var secondsToAdd = 0.8 * int.Parse(responseJson.GetValue(AccessTokenExpireKey).ToString());
                AccessTokenValidTill = DateTime.UtcNow.AddSeconds(secondsToAdd);
            }
            catch (Exception e)
            {
                throw new SystemException("Error getting LWA Access Token", e);
            }

            return accessToken;
        }

        private bool IsSuccessful(IRestResponse response)
        {
            int statusCode = (int)response.StatusCode;
            return statusCode >= 200 && statusCode <= 299 && response.ResponseStatus == ResponseStatus.Completed;
        }
    }
}
