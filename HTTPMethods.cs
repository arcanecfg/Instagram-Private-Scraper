using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace InstagramPrivateScraper
{
    class HTTPMethods
    {
        public static string GET(string url, CookieContainer cookies, string referer = "")
        {
            try
            {
                HttpWebRequest req;
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.KeepAlive = true;
                req.CookieContainer = cookies;
                req.ContentType = Constants.ContentType;
                req.UserAgent = Constants.UserAgent;
                req.Referer = referer;
                req.AllowAutoRedirect = true;

                HttpWebResponse resp;
                resp = (HttpWebResponse)req.GetResponse();
                cookies.Add(resp.Cookies);
                InstagramAccount.cookies = cookies;
                System.IO.StreamReader pageReader = new System.IO.StreamReader(resp.GetResponseStream());
                string pageSrc = pageReader.ReadToEnd();
                resp.Dispose();
                pageReader.Dispose();
                return pageSrc;
            }
            catch (WebException ex)
            {
                return ex.Message.ToString();
            }
        }

        public static bool POST(string postString, CookieContainer cookies, 
            string csrfToken, string validKey, string referer = "")
        {
            try
            {
                byte[] postData = Encoding.ASCII.GetBytes(postString);
                HttpWebRequest req;
                req = (HttpWebRequest)WebRequest.Create(Constants.PostUrl);
                req.Method = "POST";
                req.KeepAlive = true;
                req.CookieContainer = cookies;
                req.ContentType = Constants.ContentType;
                req.Accept = "*/*";
                req.UserAgent = Constants.UserAgent;
                req.ContentLength = postData.Length;
                req.Referer = referer;
                req.AllowAutoRedirect = true;
                req.Headers.Add("X-Instagram-AJAX", "1");
                req.Headers.Add("X-CSRFToken", csrfToken);
                req.Headers.Add("X-Requested-With", "XMLHttpRequest");

                System.IO.Stream postStream = req.GetRequestStream();
                postStream.Write(postData, 0, postData.Length);
                postStream.Dispose();

                HttpWebResponse resp;
                resp = (HttpWebResponse)req.GetResponse();
                cookies.Add(resp.Cookies);

                System.IO.StreamReader pageReader = new System.IO.StreamReader(resp.GetResponseStream());
                string pageSrc = pageReader.ReadToEnd();
                resp.Dispose();
                pageReader.Dispose();

                if (pageSrc.Contains(validKey))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            catch (Exception)
            {
                return false;
            }
        }

        /*
        ~HTTPMethods()
        { }
         */
    }
}
