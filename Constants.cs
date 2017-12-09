using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace InstagramPrivateScraper
{
    public static class Constants
    {
        public const string PostUrl = "https://www.instagram.com/accounts/login/ajax/";
        public const string GetUrl = "https://www.instagram.com/accounts/login/";
        public const string ContentType = "application/x-www-form-urlencoded";
        public const string UserAgent = 
            "Mozilla/5.0 (Windows NT 6.3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.154 Safari/537.36";
        public const string Referer = "https://www.instagram.com/";
        public const string title = "Instagram Private Scraper";
        public const string author = "Arcanecfg / www.WastedWolf.com";
        public const string separator = "+-------------------------------------------------+";
    }
}
