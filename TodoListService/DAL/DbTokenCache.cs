using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using TodoListService.Models;

namespace TodoListService.DAL
{

    public class DbTokenCache : TokenCache
    {
        //private DbTokenCache db = new DbTokenCache();
        string User;
        PerWebUserCache Cache;

        // constructor
        public DbTokenCache(string user)
        {
            // associate the cache to the current user of the web app
            User = user;

            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.BeforeWrite = BeforeWriteNotification;

            Cache = GetPerUserCache(User);
            // look up the entry in the DB
            //Cache = db.PerUserCacheList.FirstOrDefault(c => c.webUserUniqueId == User);
            // place the entry in memory
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }

        // clean up the DB
        public override void Clear()
        {
            base.Clear();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbTokenCache"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("DELETE FROM PerWebUserCaches WHERE webUserUniqueId = @webUserUniqueId", conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@webUserUniqueId", User);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (Cache == null)
            {
                // first time access
                Cache = GetPerUserCache(User);
                //Cache = db.PerUserCacheList.FirstOrDefault(c => c.webUserUniqueId == User);
            }
            else
            {   // retrieve last write from the DB
                PerWebUserCache cache = GetPerUserCache(User);
                // if the in-memory copy is older than the persistent copy
                if (cache.LastWrite > Cache.LastWrite)
                //// read from from storage, update in-memory copy
                {
                    Cache = cache;
                }
            }
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }
        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (this.HasStateChanged)
            {
                Cache = new PerWebUserCache
                {
                    webUserUniqueId = User,
                    cacheBits = this.Serialize(),
                    LastWrite = DateTime.Now
                };

                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbTokenCache"].ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd;
                    if (Cache.EntryId == 0)
                    {
                        cmd = new SqlCommand("INSERT INTO PerWebUserCaches (webUserUniqueId, cacheBits, LastWrite) VALUES (@webUserUniqueId, @cacheBits, @LastWrite)", conn);
                        cmd.Parameters.AddWithValue("@webUserUniqueId", Cache.webUserUniqueId);
                        cmd.Parameters.AddWithValue("@cacheBits", Cache.cacheBits);
                        cmd.Parameters.AddWithValue("@LastWrite", Cache.LastWrite);
                    }
                    else
                    {
                        cmd = new SqlCommand("UPDATE PerWebUserCaches SET webUserUniqueId = @webUserUniqueId, cacheBits = @cacheBits, LastWrite = @LastWrite WHERE EntryId = @EntryId", conn);
                        cmd.Parameters.AddWithValue("@webUserUniqueId", Cache.webUserUniqueId);
                        cmd.Parameters.AddWithValue("@cacheBits", Cache.cacheBits);
                        cmd.Parameters.AddWithValue("@LastWrite", Cache.LastWrite);
                        cmd.Parameters.AddWithValue("@EntryId", Cache.EntryId);
                    }
                    cmd.CommandType = System.Data.CommandType.Text;

                    using (cmd)
                    {
                        cmd.ExecuteNonQuery();
                    }

                }

                this.HasStateChanged = false;
            }
        }
        void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }


        PerWebUserCache GetPerUserCache(string user)
        {
            PerWebUserCache cache = null;
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DbTokenCache"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT EntryId, webUserUniqueId, cacheBits, LastWrite FROM PerWebUserCaches WHERE webUserUniqueId = @webUserUniqueId", conn))
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.AddWithValue("@webUserUniqueId", user);

                    SqlDataReader rdr = cmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        cache = new PerWebUserCache
                        {
                            EntryId = (int)rdr["EntryId"],
                            webUserUniqueId = rdr["webUserUniqueId"].ToString(),
                            cacheBits = (byte[])rdr["cacheBits"],
                            LastWrite = (DateTime)rdr["LastWrite"]
                        };
                    }
                }
            }

            return cache;
        }
    }
}
