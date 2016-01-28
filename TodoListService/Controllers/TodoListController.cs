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

using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;
using System.Web.Http.Cors;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    [Authorize]
    public class TodoListController : ApiController
    {
        //
        // To authenticate to the SQL Azure API, the app needs to know the SQL Azure API's App ID URI.
        //
        private static string armResourceId = ConfigurationManager.AppSettings["ida:ARMResourceId"];

        // GET api/todolist
        public IEnumerable<TodoItem> Get()
        {
            //
            // The Scope claim tells you what permissions the client application has in the service.
            // In this case we look for a scope value of user_impersonation, or full access to the service as the user.
            //
            if (ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope").Value != "user_impersonation")
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
            }

            // A user's To Do list is keyed off of the NameIdentifier claim, which contains an immutable, unique identifier for the user.
            Claim subject = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier);

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ToDoList"].ConnectionString))
            {
                if (!conn.ConnectionString.ToUpper().Contains("USER ID"))
                {
                    conn.AccessToken = Utils.AccessToken.GetAzureSqlAccessToken();
                }
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("SELECT ID, Title, Owner FROM ToDoItems WHERE Owner = @Owner", conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@Owner", subject.Value);

                    List<TodoItem> items = new List<TodoItem>();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        items.Add(new TodoItem
                        {
                            ID = (int)rdr["ID"],
                            Title = (string)rdr["Title"],
                            Owner = (string)rdr["Owner"]
                        });
                    }

                    return items;
                }
            }
        }

        // POST api/todolist
        public void Post(TodoItem todo)
        {
            if (ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/scope").Value != "user_impersonation")
            {
                throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The Scope claim does not contain 'user_impersonation' or scope claim not found" });
            }

            if (null != todo && !string.IsNullOrWhiteSpace(todo.Title))
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ToDoList"].ConnectionString))
                {
                    if (!conn.ConnectionString.ToUpper().Contains("USER ID"))
                    {
                        conn.AccessToken = Utils.AccessToken.GetAzureSqlAccessToken();
                    }
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("INSERT INTO ToDoItems (Title, Owner) VALUES (@Title, @Owner)", conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@Title", todo.Title);
                        cmd.Parameters.AddWithValue("@Owner", ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
