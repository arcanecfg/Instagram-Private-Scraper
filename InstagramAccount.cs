using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace InstagramPrivateScraper
{
    class InstagramAccount
    {
        public static string username;
        public static string password;
        public static CookieContainer cookies;
        public InstagramAccount(string user = "", string pwd= "", CookieContainer cookie = null)
        {
            username = user;
            password = pwd;
            cookies = cookie;
        }
    }
}
