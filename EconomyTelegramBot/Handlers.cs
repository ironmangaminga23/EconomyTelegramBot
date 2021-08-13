using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EconomyTelegramBot
{
    public class Handlers
    {
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                // UpdateType.Unknown:
                 UpdateType.ChannelPost => BotOnMessageReceived(botClient, update.Message),
                // UpdateType.EditedChannelPost:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message),
                UpdateType.MyChatMember => Abc(botClient, update.Message, update.Type),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage),
                //UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery),
                //UpdateType.InlineQuery => BotOnInlineQueryReceived(botClient, update.InlineQuery),
                //UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(botClient, update.ChosenInlineResult),
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

        private static async Task Abc(ITelegramBotClient botClient, Message message, UpdateType type)
        {
            Console.WriteLine($"{type}");
        }

        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            SendInlineKeyboard(botClient, message);
            //Console.WriteLine($"Receive message type: {message.Type}");
            //if (message.Type != MessageType.Text)
            //    return;

            //var action = (message.Text.Split(' ').First()) switch
            //{
            //    "/inline" => SendInlineKeyboard(botClient, message),
            //};
            //var sentMessage = await action;
            //Console.WriteLine($"The message was sent with id: {sentMessage.MessageId}");

            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message)
            {
                await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var question = message.Text.Trim('/');
                var answer = "Ну такое";

                if (int.TryParse(question.Split(new char[] { ' ', '/' })[0], out int sum))
                {
                    if (sum < 100)
                    { answer = "ты нищеброд!";
                    }
                    else if (sum < 500)
                    {
                        answer = "ну ок";
                            }
                    else answer = "Мы копим на дом вапщето";
                }

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: answer);
            }
        }
    }
}
