using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Extensions.Polling;

namespace EconomyTelegramBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var token = "1938522743:AAH9pUTEaDkMbo_KXzBYH5NOUA3zID5J51I";

            var bot = new TelegramBotClient(token);

            var me = await bot.GetMeAsync();
            Console.Title = me.Username;

            using var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            bot.StartReceiving(new DefaultUpdateHandler(
                Handlers.HandleUpdateAsync, 
                Handlers.HandleErrorAsync),
                               cts.Token);

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();
        }
    }
}
