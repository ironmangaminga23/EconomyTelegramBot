using System.Collections.Generic;

namespace EconomyTelegramBot.Models
{
    public class DbTable
    {

        public string Id { get; set; }

        public CurrentSummary CurrentSummary { get; set; }

        public Dictionary<int, Summary> Archive { get; set; }
    }
}
