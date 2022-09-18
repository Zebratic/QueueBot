using Discord;
using Discord.WebSocket;
using DiscordNETBotTemplate;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace QueueBot
{
    internal class Commands
    {
        public static async void Help(SocketSlashCommand arg)
        {
            EmbedBuilder embed = new EmbedBuilder();
            string content = "/help - Display all available commands\n/queueadd {token} {amount} - add a token to the sniping queue\n/restart - restart / forcestart the sniper\n/removetoken {token} - completely removes a token from the queue\n/queue - Print the current queue, AND check if their tokens are valid\n/status - Use your API key, to get stats regarding servers, alts and sniped nitro\n/check {token} - Check a token, before adding it, to make sure it's fully verified";
            embed.AddField("Command list",
                content)
                .WithAuthor(Program._client.CurrentUser)
                .WithFooter(footer => footer.Text = "Blomgreen#6969")
                .WithColor(Color.Blue)
                .WithTitle("Commands")
                .WithDescription("Here you'll find the list of commands")
                .WithCurrentTimestamp();

            await arg.RespondAsync(embed: embed.Build());
        }

        public static async void Restart(SocketSlashCommand arg)
        {
            await arg.RespondAsync("Restarting velocity, please wait.");
            Methods.forceRestart();
            if (Process.GetProcessesByName("velocitysniper").Length > 0)
                await arg.RespondAsync("Velocity found - Sniper should now be running");
        }

        public static async void AddToken(SocketSlashCommand arg)
        {
            string token = string.Empty;
            int amount = 0;
            try { amount = Convert.ToInt32(arg.Data.Options.ElementAt(0).Value); } catch { }
            try { token = arg.Data.Options.ElementAt(1).Value.ToString(); } catch { }

            if (!string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine($"Adding token: {token}, {amount} times");
                Methods.addTokenToQueue(token, amount);
                await arg.RespondAsync($"{token} has been added to the queue - Don't forget to restart the sniper");
            }
            else
            {
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Token Add Request!");
                embed.WithDescription("Click request QR Code, and then scan the QR code from your phone inside the Discord App!");
                embed.WithColor(Color.Blue);

                ComponentBuilder components = new ComponentBuilder();
                components.WithButton("Request QR Code", "REQUEST_QR_CODE", ButtonStyle.Primary);
                try { await arg.RespondAsync(embed: embed.Build(), components: components.Build()); } catch { }
            }
        }

        public static async void RemoveToken(SocketSlashCommand arg)
        {
            string token = arg.Data.Options.ElementAt(0).Value.ToString();

            string queuePath = $"{Environment.CurrentDirectory}\\queue.txt";
            if (File.ReadAllLines(queuePath).Contains(token))
            {
                List<string> tokenList = new List<string>();

                foreach (string line in File.ReadAllLines(queuePath))
                {
                    if (!line.Contains(token))
                        tokenList.Add(line);
                    else if (line.Contains(token))
                        Console.WriteLine("Token to remove detected, not adding to list");
                }

                File.WriteAllLines(queuePath, tokenList.ToArray());
                await arg.RespondAsync($"{token} has been removed from the queue - Don't forget to restart the sniper");
            }
            else
                await arg.RespondAsync($"Couldn't find {token} in queue.txt");
        }

        public static async void Queue(SocketSlashCommand arg)
        {
            string Active_Emoji = SettingsLoader.CurrentSettings.ActiveEmoji;
            string Awaiting_Emoji = SettingsLoader.CurrentSettings.AwaitingEmoji;

            List<string> queue = Methods.printQueue();
            string[] queueContent = queue.ToArray();
            string content = "";
            int firstToken = 0;
            foreach (var line in queueContent)
            {
                if (firstToken == 0)
                    content += line + Active_Emoji + "\n";

                content += line + Awaiting_Emoji + "\n";
            }

            if (string.IsNullOrWhiteSpace(content))
                content = "No users in queue!";

            var embed = new EmbedBuilder();
            // Or with methods
            embed.AddField("Users in queue", content)
                .WithAuthor(Program._client.CurrentUser)
                .WithFooter(footer => footer.Text = "Blomgreen#6969")
                .WithColor(Color.Blue)
                .WithTitle("Queue list")
                .WithDescription("Here you'll find the current queue, including the amount of snipes remaining.")
                .WithCurrentTimestamp();

            //Your embed needs to be built before it is able to be sent
            await arg.RespondAsync(embed: embed.Build());
        }

        public static async void Stats(SocketSlashCommand arg)
        {
            WebClient wc = new WebClient();
            string rawData = wc.DownloadString($"https://genefit.to/velocity/api/stats?key={SettingsLoader.CurrentSettings.VelocityApiKey}");
            Console.WriteLine(rawData);
            VelocityResponse response = JsonConvert.DeserializeObject<VelocityResponse>(rawData);
            if (response.Success)
            {
                Console.WriteLine($"Servers: {response.TotalServers}");
                Console.WriteLine($"Alts: {response.Alts}");
                Console.WriteLine($"Nitro Sniped: {response.NitroClaimed}");
                string content = $"Servers: {response.TotalServers}\nAlts: {response.Alts}\nNitro Sniped: {response.NitroClaimed}";


                var embed = new EmbedBuilder();
                // Or with methods
                embed.AddField("Stats",
                    content)
                    .WithAuthor(Program._client.CurrentUser)
                    .WithFooter(footer => footer.Text = "Blomgreen#6969")
                    .WithColor(Color.Blue)
                    .WithTitle("Status")
                    .WithDescription("Here you'll find the sniper stats")
                    .WithCurrentTimestamp();

                //Your embed needs to be built before it is able to be sent
                await arg.RespondAsync(embed: embed.Build());
            }
        }

        public static async void Check(SocketSlashCommand arg)
        {
            try
            {
                string token = arg.Data.Options.ElementAt(0).Value.ToString();

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

                Console.WriteLine(result);
                string username = json.username;
                string discriminator = json.discriminator;

                string discordID = json.id;
                string email = json.email;

                string verified = json.verified;
                string phoneNum = json.phone;

                var embed = new EmbedBuilder();
                string content = $"**Token:** {token}\n**Username:** {username}#{discriminator}\n**ID:** {discordID}\n**Verified?:** {verified}\n**PhoneNumber:** {phoneNum}\n**Mail:** {email}";
                embed.AddField("Checked Token",
                    content)
                    .WithAuthor(Program._client.CurrentUser)
                    .WithFooter(footer => footer.Text = "Blomgreen#6969")
                    .WithColor(Color.Blue)
                    .WithTitle("")
                    .WithDescription("")
                    .WithCurrentTimestamp();

                await arg.RespondAsync(embed: embed.Build());
            }
            catch (Exception ex) { await arg.RespondAsync("An error has occured: " + ex.Message); }
        }
    }
}
