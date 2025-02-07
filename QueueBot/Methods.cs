﻿using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace QueueBot
{
    class Utils
    {
        public static void sendWebHook(string URL, string msg, string username)
        {
            Http.Post(URL, new NameValueCollection()
            {
                { "username", username },
                { "content", msg },
            });
        }
    }
    internal class Http
    {
        public static byte[] Post(string uri, NameValueCollection pairs)
        {
            byte[] numArray;
            using (WebClient webClient = new WebClient())
            {
                numArray = webClient.UploadValues(uri, pairs);
            }
            return numArray;
        }
    }
    class Methods : ModuleBase<SocketCommandContext>
    {

        public static void addTokenToQueue(string token, int amount)
        {
            string queuePath = $"{Environment.CurrentDirectory}\\queue.txt";
            string velocityPath = $"{Environment.CurrentDirectory}\\velocitysniper.exe";

            Console.WriteLine("Trying to close existing velocity");

            //Try to find and close process with the name 'velocitysniper'
            try
            {
                foreach (var process in Process.GetProcessesByName("velocitysniper"))
                {
                    process.Kill();
                    Console.WriteLine("Closed velocitysniper.exe");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not find an open velocity program");
            }


            //Add new tokens to queue

            Console.WriteLine("Trying to add token to queue");
            try
            {
                int b = 1;
                for (int i = 0; i < amount; i++)
                {

                    File.AppendAllText(queuePath, token + Environment.NewLine);
                    Console.WriteLine($"Added token {b} times");
                    b++;

                }
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to add token");
            }



            //Open velocitysniper again
            Console.WriteLine("Trying to open velocitysniper");
            try
            {
                Process.Start(velocityPath);
                Console.WriteLine("Velocity should be open now");
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to open velocity");
            }
        }

        public static void forceRestart()
        {

            string velocityPath = $"{Environment.CurrentDirectory}\\velocitysniper.exe";

            //Try to find and close process with the name 'velocitysniper'
            try
            {
                foreach (var process in Process.GetProcessesByName("velocitysniper"))
                {
                    process.Kill();
                    Console.WriteLine("Closed velocitysniper.exe");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not find an open velocity program");
            }

            //Open velocitysniper again
            Console.WriteLine("Trying to open velocitysniper");
            try
            {
                Process.Start(velocityPath);
                Console.WriteLine("Velocity should be open now");

            }

            catch (Exception)
            {
                Console.WriteLine("Failed to open velocity");
            }

        }

        public static List<string> printQueue()
        {
            string queuePath = $"{Environment.CurrentDirectory}\\queue.txt";

            string[] allTokensRaw = File.ReadAllLines(queuePath);

            List<string> queueList = new List<string>();

            string[] tokenList = allTokensRaw.Distinct().ToArray();

            foreach (string token in tokenList)
            {
                string discordID = "Token invalid";
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://discord.com/api/v9/users/@me");
                    request.ContentType = "application/json";
                    request.Method = WebRequestMethods.Http.Get;
                    request.Timeout = 20000;
                    request.Headers = new WebHeaderCollection() { { "Authorization", token } };

                    WebResponse response = request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(responseStream);
                    string result = sr.ReadToEnd();

                    dynamic json = JsonConvert.DeserializeObject(result);
                    discordID = json.id;
                }
                catch { }


                int i = 0;
                foreach (string line in allTokensRaw)
                    if (line == token)
                        i++;

                queueList.Add($"User: <@{discordID}>  -  Snipes Remaining: {i}");
            }

            var queue = new List<string>();

            foreach (string finalLine in queueList.ToArray())
            {
                Console.WriteLine(finalLine);
                queue.Add(finalLine);
            }
            return queue;
        }

        public static void successWebhook()
        {
            string link = File.ReadAllLines($"{Environment.CurrentDirectory}\\config\\webhookLink.txt")[0];
            Utils.sendWebHook(link, "NITRO CLAIMED! :gift:", "DetectionBot");
        }
    }
}