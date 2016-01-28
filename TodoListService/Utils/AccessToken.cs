using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using TodoListService.DAL;

namespace TodoListService.Utils
{
    public static class AccessToken
    {
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        private static string appKey = ConfigurationManager.AppSettings["ida:AppKey"];
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];

        internal static string GetGraphAccessToken()
        {
            return GetAccessToken(ConfigurationManager.AppSettings["ida:GraphResourceId"]);
        }

        internal static string GetAzureSqlAccessToken()
        {
            return GetAccessToken(ConfigurationManager.AppSettings["ida:SQLAzureResourceId"]);
        }

        private static string GetAccessToken(string resourceId)
        {
            string accessToken = null;
            //
            // Use ADAL to get a token On Behalf Of the current user.  To do this we will need:
            //      The Resource ID of the service we want to call.
            //      The current user's access token, from the current request's authorization header.
            //      The credentials of this application.
            //      The username (UPN or email) of the user calling the API
            //
            ClientCredential clientCred = new ClientCredential(clientId, appKey);
            var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;
            string userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn) != null ? ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value : ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email).Value;
            string userAccessToken = bootstrapContext.Token;
            UserAssertion userAssertion = new UserAssertion(bootstrapContext.Token, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

            string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            AuthenticationContext authContext = new AuthenticationContext(authority, new DbTokenCache(userId));

            // In the case of a transient error, retry once after 1 second, then abandon.
            // Retrying is optional.  It may be better, for your application, to return an error immediately to the user and have the user initiate the retry.
            bool retry = false;
            int retryCount = 0;

            do
            {
                retry = false;
                try
                {
                    AuthenticationResult result = authContext.AcquireToken(resourceId, clientCred, userAssertion);
                    accessToken = result.AccessToken;
                    //AuthenticationResult result = authContext.AcquireToken(sqlAzureResourceId, clientCred, userAssertion);
                    ////result = authContext.AcquireToken(graphResourceId, clientCred, userAssertion);
                    //accessToken = result.AccessToken;
                    //AuthenticationResult result1 = authContext.AcquireToken(armResourceId, clientCred, userAssertion);
                    //AuthenticationResult result2 = authContext.AcquireTokenByRefreshToken(result1.RefreshToken, clientCred, sqlAzureResourceId);
                    //accessToken = result2.AccessToken;
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        // Transient error, OK to retry.
                        retry = true;
                        retryCount++;
                        Thread.Sleep(1000);
                    }
                }
            } while ((retry == true) && (retryCount < 1));

            return accessToken;
        }

    }
}