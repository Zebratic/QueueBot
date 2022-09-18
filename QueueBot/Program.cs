using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using QueueBot;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Color = Discord.Color;
using File = System.IO.File;

namespace DiscordNETBotTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            SettingsLoader.LoadSettings();

            Thread tr = new Thread(() => new Program().RunBotAsync().GetAwaiter().GetResult());
            tr.Start();
            tr.Join();
        }

        public static DiscordSocketClient _client;

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += _client_Log;
            _client.Ready += _client_Ready;
            _client.ButtonExecuted += _client_ButtonExecuted;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
            _client.MessageReceived += _client_MessageReceived;


            string token = "";
            try { token = File.ReadAllText(@"config\bot token.txt"); } catch { Console.WriteLine("-> 'config\\bot token.txt' is missing!\nEnter token manually? Paste it below and click enter!"); token = Console.ReadLine(); }

            try { await _client.LoginAsync(TokenType.Bot, token); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                Console.WriteLine();
                Console.WriteLine("Token not valid, or no Internet connection!");
                Console.ReadLine();
                Environment.Exit(1);
            }

            await _client.StartAsync();

            await Task.Delay(-1); // infinite wait
        }

        private async Task _client_Ready()
        {
            Console.WriteLine("Bot is online and working perfectly fine!");
            new Thread(RPC).Start(); // start the RPC messages

            // setup commands
            var cmd_help = new SlashCommandBuilder();
            cmd_help.WithName("help");
            cmd_help.WithDescription("See the help page");

            var cmd_restart = new SlashCommandBuilder();
            cmd_restart.WithName("restart");
            cmd_restart.WithDescription("Restart sniper");

            var cmd_addtoken = new SlashCommandBuilder();
            cmd_addtoken.WithName("addtoken");
            cmd_addtoken.WithDescription("Add a token to the queue");
            cmd_addtoken.AddOption("token", ApplicationCommandOptionType.String, "The token you want to add", false);
            cmd_addtoken.AddOption("amount", ApplicationCommandOptionType.Integer, "How many times you want to add the token", true);

            var cmd_removetoken = new SlashCommandBuilder();
            cmd_removetoken.WithName("removetoken");
            cmd_removetoken.WithDescription("Remove a token to the queue");
            cmd_removetoken.AddOption("token", ApplicationCommandOptionType.String, "The token you want to remove", true);

            var cmd_queue = new SlashCommandBuilder();
            cmd_queue.WithName("queue");
            cmd_queue.WithDescription("Show the current queue");

            var cmd_stats = new SlashCommandBuilder();
            cmd_stats.WithName("stats");
            cmd_stats.WithDescription("Show the current sniper stats");

            var cmd_check = new SlashCommandBuilder();
            cmd_check.WithName("check");
            cmd_check.WithDescription("Check a token");
            cmd_check.AddOption("token", ApplicationCommandOptionType.String, "The token you want to check", true);


            // register the commands
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(cmd_help.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_restart.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_addtoken.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_removetoken.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_queue.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_stats.Build());
                await _client.CreateGlobalApplicationCommandAsync(cmd_check.Build());
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg); // log discord.net specific messages (NOT CHAT)
            return Task.CompletedTask;
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            Console.WriteLine(arg.CommandName);
            if (SettingsLoader.CurrentSettings.AdminIds.Count > 0)
            {

                if (SettingsLoader.CurrentSettings.AdminIds.Contains(arg.User.Id)) // check perms
                {
                    switch (arg.CommandName)
                    {
                        case "help": Commands.Help(arg); break;
                        case "restart": Commands.Restart(arg); break;
                        case "addtoken": Commands.AddToken(arg); break;
                        case "removetoken": Commands.RemoveToken(arg); break;
                        case "queue": Commands.Queue(arg); break;
                        case "stats": Commands.Stats(arg); break;
                        case "check": Commands.Check(arg); break;
                    }
                }
                else
                    await arg.RespondAsync("You don't have permission to use this command!");
            }
            else
                await arg.RespondAsync();
        }

        private async Task _client_ButtonExecuted(SocketMessageComponent arg)
        {
            try
            {
                Console.WriteLine(arg.Data.CustomId);
                switch (arg.Data.CustomId)
                {
                    case "REQUEST_QR_CODE":
                        // update message to show that the token is being added
                        await arg.Message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        {
                            Title = "Fetching QR Code...",
                            Description = "Please wait...",
                            Color = Color.Blue
                        }.Build());

                        // get qr code from selenium
                        Client client = new Client();
                        client.Initialize();
                        client.OnTimeout += Client_OnTimeout;
                        client.OnLoggedIn += Client_OnLoggedIn;
                        client.OnQRCodeScanned += Client_OnQRCodeScanned;
                        if (client.Driver != null)
                        {
                            string qrCode = client.GetQRCode();
                            File.WriteAllBytes("qr.png", Convert.FromBase64String(qrCode.Replace("data:image/png;base64,", "")));
                            string path = Path.GetFullPath("qr.png");

                            EmbedBuilder embed = new EmbedBuilder();
                            embed.WithTitle("Here is your QR Code");
                            embed.WithDescription("Scan the QR Code from the discord app on your phone.");
                            await arg.Message.DeleteAsync();
                            client.Message = await arg.Channel.SendFileAsync(path, embed: embed.Build());
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void Client_OnQRCodeScanned(object sender, Client client, string avatar_url)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Hello USER");
            embed.WithDescription("Now click accept to verify ownership of the account!");
            embed.WithImageUrl(avatar_url);

            await client.Message.ModifyAsync(x => x.Embed = embed.Build());
        }

        private void Client_OnLoggedIn(object sender, Client client)
        {

        }

        private void Client_OnTimeout(object sender, Client client)
        {
            // respond with timed out
            Console.WriteLine("QR Code Timed out!");
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            try
            {
                var message = arg as SocketUserMessage;
                var context = new SocketCommandContext(_client, message);

                if (context.IsPrivate) // DM Message
                {
                    var colors = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"{DateTime.Now.Hour}-{DateTime.Now.Minute}|[{message.Author}|{message.Author.Id}]:\n");
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine("I like, detected a message in dms");
                    Console.WriteLine(message.Content);
                    Console.ForegroundColor = colors;
                }
                else if (message.Author.IsWebhook)
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"{DateTime.Now.Hour}-{DateTime.Now.Minute}|[{context.Guild.Id}|{context.Guild}|{message.Channel.Id}|{message.Channel}|{message.Author}|{message.Author.Id}|]:\n");
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine("I like, detected a webhook message in da chat");
                    Console.WriteLine(message.Content);
                    Console.ForegroundColor = color;


                    try
                    {
                        if (File.ReadAllText($"{Environment.CurrentDirectory}\\config\\successWebhookID.txt").Contains(context.User.Id.ToString()))
                        {
                            Console.WriteLine("Sending success webhook");
                            Methods.successWebhook();
                        }

                    }
                    catch (Exception)
                    {
                        Console.WriteLine("probably missing ID or webhook link, idk");
                        throw;
                    }

                }
                else // Everywhere else
                {
                    var color = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"{DateTime.Now.Hour}-{DateTime.Now.Minute}|[{context.Guild.Id}|{context.Guild}|{message.Channel.Id}|{message.Channel}|{message.Author}|{message.Author.Id}|]:\n");
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    Console.WriteLine(message.Content);
                    Console.ForegroundColor = color;
                }
            }
            catch
            {
                Console.WriteLine("Discord Embed Message Error!"); // not important, just keep it
            }
        }

        private async void RPC() // cycle between messages
        {
            await _client.SetGameAsync("Queue bot by Blomgreen");
        }
    }
}