//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// The following using statements were added for this sample.
using System.Threading.Tasks;
using System.Security.Claims;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Configuration;
using TodoListService.Models;
using System.Web.Http;

namespace TodoListService.Controllers
{
    [Authorize]
    public class UserProfileController : ApiController
    {
        private string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];

        //
        // GET: /UserProfile/
        public async Task<UserProfile> GetUserProfile()
        {
            UserProfile profile;

            string accessToken = Utils.AccessToken.GetGraphAccessToken();
            string tenantId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            //
            // Call the Graph API and retrieve the user's profile.
            //
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                graphUserUrl,
                HttpUtility.UrlEncode(tenantId));
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            //
            // Return the user's profile in the view.
            //
            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
                profile.DisplayName += " (returned from service)";
            }
            else
            {
                throw new HttpResponseException(Request.CreateErrorResponse(System.Net.HttpStatusCode.Unauthorized, "Unauthorized"));
            }

            return profile;
        }
    }
}