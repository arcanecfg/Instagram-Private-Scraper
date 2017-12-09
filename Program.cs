/*
 * Title:       Instagram Private Scraper
  
 * Author:      Arcanecfg
                www.WastedWolf.com
                www.YouTube.com/Arcanecfg
  
 * Description: Scrape public and private photos from Instagram profiles
                without using the official API.
    
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace InstagramPrivateScraper
{
    class Program
    {
        static string targetUser;
        static void Main(string[] args)
        {
            DisplayLoadInfo();
            string repeatAnswer = "n";
            do
            {
                Console.ForegroundColor = ConsoleColor.White;
                InstagramAccount.cookies = new CookieContainer();
                Console.Write("Target Username: ");
                targetUser = Console.ReadLine();

                //Start FetchPosts() as a separate task
                Task fetchTask = new Task(FetchPosts);
                fetchTask.Start();

                //Wait until FetchPosts() is complete
                Console.WriteLine("Fetching profile info...\n");
                fetchTask.Wait();

                Console.WriteLine("Done.");
                Console.Write("\nPerform another scrape? (y/n): ");
                repeatAnswer = Console.ReadLine();
            } while (repeatAnswer == "y");
        }

        private static void FetchPosts()
        {
            List<string> postList = new List<string>();
            
            //The ?__a=1 parameter returns just the JSON without any HTML
            string pageUrl = "https://www.instagram.com/" + targetUser + "/?__a=1";

            //Flag separates the first iteration from the rest
            bool flag = false;

            //WebClient used for downloading the webpage initially
            WebClient wc = new WebClient();
            try
            {
                string rawSrc = wc.DownloadString("https://www.instagram.com/" + targetUser + "/?__a=1");
               
                Console.WriteLine("Name: {0}", ParseText(rawSrc, "\"full_name\": \"", "\", \"has_blocked_viewer"));

                string mediaCount;

                //Private account
                if (rawSrc.Contains("\"media\": {\"nodes\": [], \"count\": "))
                {
                    mediaCount = ParseText(rawSrc, "\"media\": {\"nodes\": [], \"count\": ", ", \"page_info\"");
                }
                //Public account
                else
                {
                    mediaCount = ParseText(rawSrc, "}}], \"count\": ", ", \"page_info");
                }
                Console.WriteLine("Photos: {0}", mediaCount);
                Console.WriteLine("Biography: {0}", ParseText(rawSrc, "\"biography\": \"", "\", \"blocked_by_viewer"));
            
                if (rawSrc.Contains("\"is_private\": true"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\nAccount is private; please login to your account.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Username: ");
                    InstagramAccount.username = Console.ReadLine();
                    Console.Write("Password: ");
                    InstagramAccount.password = Console.ReadLine();

                    //Task to login to your account
                    Task loginTask = new Task(LogIn);
                    loginTask.Start();
                    Console.WriteLine("\nLogging in...");
                    loginTask.Wait();
                }

                //queryId is a constant stored in the ConsumerCommons.js file — no longer required
                //Console.WriteLine("Fetching query ID...");
                //string rawJs = new WebClient().DownloadString("https://www.instagram.com/static/bundles/ConsumerCommons.js/db149d8f0b6c.js");
                //string queryId = ParseText(rawJs, "queryId:\"", "\",");

                //profileId was required for the first endpoint — no longer required.
                //string profileId = ParseText2(rawSrc, "\"owner\": {\"id\": \"", "\"},");
                //Console.WriteLine("Query ID: " + queryId);
                Console.WriteLine("\nFetching posts...");

                do
                {
                    rawSrc = HTTPMethods.GET(pageUrl, InstagramAccount.cookies, "https://www.instagram.com/" + targetUser + "/");
                    string json;
                    //First iteration
                    if (rawSrc.Contains("\"media\": {\"nodes\": "))
                    {
                        //Split the raw string into a valid JSON format
                        json = ParseText(rawSrc, "\"media\": {\"nodes\": ", "}}], \"count\"") + "}}]";
                        //flag=false indicates first iteration
                        flag = false;
                    }
                    else
                    {
                        //JSON keys are different from first iteration
                        json = ParseText(rawSrc, "\"edges\": ", "}]}}}") + "}]";
                        //flag=true indicates all other iterations
                        flag = true;
                    }
                    string end_cursor = string.Empty;

                    //If there are more pages with posts
                    if (rawSrc.Contains("\"has_next_page\": true"))
                    {
                        end_cursor = ParseText(rawSrc, "{\"has_next_page\": true, \"end_cursor\": \"", "\"}");
                    }

                    //Deserialize JSON
                    dynamic dyn = JsonConvert.DeserializeObject(json);
                    foreach (var post in dyn)
                    {
                        if (flag == false)
                            //First iteration uses display_src key
                            postList.Add((string)post.display_src + "$" + (string)post.code);
                        else
                            //Subsequent iterations use display_url
                            postList.Add((string)post.display_url + "$" + (string)post.code);
                    }
                    //Pagination for multiple pages
                    pageUrl = "https://www.instagram.com/" + targetUser + "/?__a=1&max_id=" + end_cursor;
                    //The original endpoint I was using — complicated and unnecessary
                    //pageUrl = "https://www.instagram.com/graphql/query/?query_id="+ queryId +"&variables={\"id\":\""+ profileId + "\",\"first\":12,\"after\":\"" + end_cursor + "\"}" ;

                }
                while (rawSrc.Contains("\"has_next_page\": true"));

                Console.WriteLine("Finished fetching {0} posts.", postList.Count);

                //Create new directory for user
                if (!Directory.Exists(targetUser))
                    Directory.CreateDirectory(targetUser);
                Console.WriteLine("Created new directory {0}", targetUser);

                DownloadMedia(postList);
                postList.Clear();
            }
            catch (Exception ex)
            { 
                Console.WriteLine("\nAn error occured: {0}", ex.Message); 
            }
           
        }

        private static void LogIn()
        {
            try
            {
                //Fetch csrftoken using a GET request and store the cookies
                var csrfToken = ParseText(HTTPMethods.GET(Constants.GetUrl, InstagramAccount.cookies, Constants.Referer), "csrf_token\": \"", "\", \"viewer");

                //Use the fetched csrftoken and saved cookies for logging in
                if (HTTPMethods.POST("username=" + InstagramAccount.username + "&password=" + InstagramAccount.password, InstagramAccount.cookies, csrfToken, "\"authenticated\": true", Constants.Referer))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Successfully logged in");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error logging in.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while logging in: {0}", ex.Message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void DownloadMedia(List<string>postList)
        {
            int downloadCounter = 0;
            WebClient picDownloader;
            Console.WriteLine("Starting download...");
            Console.ForegroundColor = ConsoleColor.DarkGreen;

            //Repeat for all posts in postList
            Parallel.ForEach(postList, post =>
            {
                //postInfo contains post's shortcode and URL
                string[] postInfo = post.Split('$');
                picDownloader = new WebClient();
                picDownloader.DownloadFile(postInfo[0], targetUser + @"\" + postInfo[1] + ".jpg");
                downloadCounter++;
                Console.WriteLine("[{1}/{2}] Downloaded {0}.jpg", postInfo[1], downloadCounter, postList.Count);
            });
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static string ParseText(string value, string a, string b)
        {
            try
            {
                //Index of first character of first string
                int posA = value.IndexOf(a);

                //Only check for beginning of second string starting from the index of first string
                int posB = value.Substring(posA).IndexOf(b) + posA; //Add posA to get the appropriate index

                //Skip the indices containing the rest of the first string
                posA += a.Length;
                return value.Substring(posA, posB - posA);
            }
            catch { return "?"; }
        }

        static void CenterPrintString(string msg)
        {
            Console.SetCursorPosition((Console.WindowWidth - msg.Length) / 2, Console.CursorTop);
            Console.WriteLine(msg);
        }

        static void DisplayLoadInfo()
        {
            Console.Title = Constants.title;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(Environment.NewLine);
            CenterPrintString(Constants.title);
            Console.ForegroundColor = ConsoleColor.White;
            CenterPrintString(Constants.author);
            CenterPrintString("Release: 09/12/2017");
            CenterPrintString(Constants.separator);
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
