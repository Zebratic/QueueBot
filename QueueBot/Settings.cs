using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace QueueBot
{
    public class Settings
    {
        public string DiscordGetTokenScript = "(webpackChunkdiscord_app.push([[''],{},e=>{m=[];for(let c in e.c)m.push(e.c[c])}]),m).find(m=>m?.exports?.default?.getToken!==void 0).exports.default.getToken()";
        public string QrCodeOffset = "/html/body/div[1]/div[2]/div/div[1]/div/div/div/div/form/div/div/div[3]/div/div/div/div[1]/div[1]/img";
        public string AvatarUrlOffset = "/html/body/div[1]/div[2]/div/div[1]/div/div/div/div/form/div/div/div[3]/div/div/div/div[1]/svg/foreignObject/div/img";
        public bool HeadlessDriver = true;
        public int QRCodeTimeout = 90;
        public string VelocityApiKey = "";
        public string ActiveEmoji = "";
        public string AwaitingEmoji = "";
        public List<ulong> AdminIds = new List<ulong>();
    }

    public class SettingsLoader
    {
        public static Settings CurrentSettings = null;
        public static void LoadSettings()
        {
            if (CurrentSettings == null)
                CurrentSettings = new Settings();

            if (File.Exists("Settings.json"))
            {
                string content = File.ReadAllText("Settings.json");
                CurrentSettings = JsonConvert.DeserializeObject<Settings>(content);
                Console.WriteLine("[SETTINGS] -> LOADED SETTINGS FROM \"Settings.json\"!");
            }
            else
            {
                SaveSettings();
                Console.WriteLine("[SETTINGS] -> CANT LOAD SETTINGS SINCE \"Settings.json\" IS MISSING!");
            }
        }

        public static void SaveSettings()
        {
            string content = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
            File.WriteAllText("Settings.json", content);
            Console.WriteLine("[SETTINGS] -> SAVED SETTINGS TO \"Settings.json\"!");
        }
    }
}
