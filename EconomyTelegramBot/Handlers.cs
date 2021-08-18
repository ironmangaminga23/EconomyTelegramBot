using EconomyTelegramBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EconomyTelegramBot
{
    public class Handler
    {
        private DbTable table;
        private HttpClient client;
        string db;

        public Handler()
        {
            client = new HttpClient();
            db = "https://economyvasdb-default-rtdb.firebaseio.com/db/db/-MhPag0TO31nSnwEl2yj";
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.ChannelPost => BotOnMessageReceived(botClient, update.Message),
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                UpdateType.MyChatMember => Abc(botClient, update.Message, update.Type),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage),
                _ => Abc(botClient, update.Message, update.Type)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }

        private async Task Abc(ITelegramBotClient botClient, Message message, UpdateType type)
        {
            Console.WriteLine($"{type}");
        }

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            using (var client = new HttpClient())
            {
                var res = await client.GetAsync($"{db}.json");
                var content = await res.Content.ReadAsStringAsync();
                var responseBody = JsonConvert.DeserializeObject<DbTable>(content);

                //if (responseBody == null)
                //{
                //    table = new DbTable()
                //    {
                //        CurrentSummary = new CurrentSummary()
                //        {
                //            Sum = new Summary()
                //            {
                //                CommonSum = 0,
                //                CommonAlko = 0,
                //                CommonCoffee = 0,
                //                CommonEntertainment = 0
                //            },
                //            CurrentMonth = DateTime.Now.Month
                //        },
                //        Archive = new Dictionary<int, Summary>() { }
                //    };

                //    string json = JsonConvert.SerializeObject(table);
                //    StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                //    var r = await client.PostAsync($"{db}/db.json", httpContent);
                //}

                if (message.Type != MessageType.Text)
                    return;

                var test = message.Text.Split(new char[] { ' ', '/' });
                Task action = message.Text switch
                {
                    "/help" => GetHelpInfo(botClient, message),
                    "/anal" => SendAnal(botClient, message, responseBody.CurrentSummary.Sum),
                    "/full_anal" => SendFullAnal(botClient, message, responseBody?.Archive ?? new Dictionary<int, Summary>()),
                    _ => int.TryParse(message.Text.Split(new char[] { ' ', '/' })[1], out int sum)
                        ? SendInlineKeyboard(botClient, message, responseBody, sum)
                        : NotUderstand(botClient, message)
                };

                await action;
            }
        }

        private async Task NotUderstand(ITelegramBotClient botClient, Message message)
        {
            var answer = "Я не понял что ты написал";
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                text: answer);
        }

        private async Task GetHelpInfo(ITelegramBotClient botClient, Message message)
        {
            var answer = "/help - помощь /anal - сводка за месяц  /full_anal - полная сводка";
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: answer);
        }

        private async Task SendFullAnal(
            ITelegramBotClient botClient, Message message, Dictionary<int, Summary> archive)
        {
            foreach (var m_summary in archive)
            {
                string answer = $"Месяц - {m_summary.Key.ToMonth()}" +
                    $" Полнная сумма - {m_summary.Value.CommonSum} Алкоголь - {m_summary.Value.CommonAlko}" +
                    $" Развлекухи - {m_summary.Value.CommonEntertainment} Кофе - {m_summary.Value.CommonCoffee}";
                await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: answer);
            }
        }

        private async Task SendInlineKeyboard(
            ITelegramBotClient botClient, Message message, DbTable table, int specialSum)
        {
            DbTable newTable = table;
            int current = DateTime.Now.Month;

            await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            var question = message.Text.Trim('/');

            if (current == table.CurrentSummary.CurrentMonth)
            {
                newTable.CurrentSummary.Sum.CommonSum += specialSum;
                if (question.Contains("#coffee"))
                {
                    newTable.CurrentSummary.Sum.CommonCoffee += specialSum;
                }
                else if (question.Contains("#entertainment"))
                {
                    newTable.CurrentSummary.Sum.CommonEntertainment += specialSum;
                }
                else if (question.Contains("#alko"))
                {
                    newTable.CurrentSummary.Sum.CommonAlko += specialSum;
                }
                else if (question.Contains("#"))
                {
                    var answer = "Я не понял что за хэштег";
                    await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: answer);
                }
            }
            else
            {
                if (table.Archive == null)
                {
                    table.Archive = new Dictionary<int, Summary>();
                }
                table.Archive.Add(newTable.CurrentSummary.CurrentMonth, newTable.CurrentSummary.Sum);

                newTable.CurrentSummary.CurrentMonth = current;
                newTable.CurrentSummary.Sum.CommonSum = specialSum;
                if (question.Contains("#coffee"))
                {
                    newTable.CurrentSummary.Sum.CommonCoffee = specialSum;
                }
                else if (question.Contains("#entertainment"))
                {
                    newTable.CurrentSummary.Sum.CommonEntertainment = specialSum;
                }
                else if (question.Contains("#alko"))
                {
                    newTable.CurrentSummary.Sum.CommonAlko = specialSum;
                }
                else if (question.Contains("#"))
                {
                    var answer = "Я не понял что за хэштег";
                    await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                        text: answer);
                }
            }

            string json = JsonConvert.SerializeObject(newTable);
            StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            await client.PatchAsync($"{db}.json", httpContent);
        }

        private async Task SendAnal(ITelegramBotClient botClient, Message message, Summary summary)
        {
            string answer = $"Полнная сумма - {summary.CommonSum} Алкоголь - {summary.CommonAlko}" +
                $" Развлекухи - {summary.CommonEntertainment} Кофе - {summary.CommonCoffee}";
            await botClient.SendTextMessageAsync(chatId: message.Chat.Id, text: answer);
        }
    }
}
