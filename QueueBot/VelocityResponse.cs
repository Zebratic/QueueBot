using Newtonsoft.Json;
using System;

namespace QueueBot
{
    public partial class VelocityResponse
    {

        [JsonProperty("total_servers")]
        public long TotalServers { get; set; }

        [JsonProperty("alts")]
        public long Alts { get; set; }

        [JsonProperty("nitro_claimed")]
        public long NitroClaimed { get; set; }

        [JsonProperty("time_running")]
        public DateTimeOffset TimeRunning { get; set; }

        [JsonProperty("giveaways_joined")]
        public long GiveawaysJoined { get; set; }

        [JsonProperty("giveaways_won")]
        public long GiveawaysWon { get; set; }

        [JsonProperty("last_update")]
        public long LastUpdate { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
