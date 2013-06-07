using DotNetOpenAuth.AspNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace LucasCanavan.MicrosoftAccountAuthenticationClient
{
    public class MicrosoftAccountAuthenticationClient : IAuthenticationClient
    {
        protected string _clientId;
        protected string _clientSecret;
        protected string _scopes;

        protected const string PROVIDER_NAME = "microsoft";
        protected const string AUTHORIZATION_TEMPLATE = "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";
        protected const string ACCESS_TOKEN_ENDPOINT = "https://login.live.com/oauth20_token.srf";
        protected const string ACCESS_TOKEN_TEMPLATE = "client_id={0}&redirect_uri={1}&client_secret={2}&code={3}&grant_type=authorization_code";
        protected const string USER_TEMPLATE = "https://apis.live.net/v5.0/me?access_token={0}";

        public MicrosoftAccountAuthenticationClient(string clientId, string clientSecret, string scopes)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scopes = scopes;
        }
        
        string IAuthenticationClient.ProviderName
        {
            get { return PROVIDER_NAME; }
        }

        void IAuthenticationClient.RequestAuthentication(HttpContextBase context, Uri returnUrl)
        {
            string url = String.Format(AUTHORIZATION_TEMPLATE, _clientId, _scopes, HttpUtility.UrlEncode(returnUrl.ToString()));
            context.Response.Redirect(url);
        }

        AuthenticationResult IAuthenticationClient.VerifyAuthentication(HttpContextBase context)
        {
            try
            {
                // Extract parameters.
                string code = context.Request.QueryString["code"] ?? String.Empty;
                string rawUrl = context.Request.Url.AbsoluteUri;

                // Remove code portion from the url.
                rawUrl = Regex.Replace(rawUrl, "&code=[^&]*", "");

                // Request to obtain access token, authentication token, etc.            
                var tokenRequestData = String.Format(ACCESS_TOKEN_TEMPLATE, _clientId, HttpUtility.UrlEncode(rawUrl), _clientSecret, code);

                WebRequest tokenRequest = WebRequest.Create(ACCESS_TOKEN_ENDPOINT);
                tokenRequest.ContentType = "application/x-www-form-urlencoded";
                tokenRequest.ContentLength = tokenRequestData.Length;
                tokenRequest.Method = "POST";

                using (Stream requestStream = tokenRequest.GetRequestStream())
                {
                    var writer = new StreamWriter(requestStream);
                    writer.Write(tokenRequestData);
                    writer.Flush();
                }

                var tokenData = new Dictionary<string, string>();
                HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();
                if (tokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    var json = String.Empty;
                    using (Stream responseStream = tokenResponse.GetResponseStream())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            json = sr.ReadToEnd();
                        }
                    }
                    tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }


                // Request to obtain user providerUserId, userName, etc.
                var accessToken = String.Empty;
                tokenData.TryGetValue("access_token", out accessToken);

                var userUri = String.Format(USER_TEMPLATE, accessToken);

                var userData = new Dictionary<string, object>();

                var userRequest = WebRequest.Create(userUri);
                using (var userResponse = userRequest.GetResponse())
                {
                    var json = String.Empty;
                    using (var responseStream = userResponse.GetResponseStream())
                    {
                        using (var sr = new StreamReader(responseStream))
                        {
                            json = sr.ReadToEnd();
                        }
                    }
                    userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }

                // Append the user data to the token data so we can return everything in a single dictionary.
                foreach (var item in userData)
                {
                    if (item.Value != null)
                    {
                        tokenData.Add(item.Key, item.Value.ToString());
                    }
                }

                var providerUserId  = String.Empty;
                tokenData.TryGetValue("id", out providerUserId);

                var userName  = String.Empty;
                tokenData.TryGetValue("name", out userName);

                return new AuthenticationResult(true, PROVIDER_NAME, providerUserId, userName, tokenData);
            }
            catch (Exception ex)
            {
                return new AuthenticationResult(ex);
            }
        }
    }
}