using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class SetMailCommand : Command
    {
        public override string Name { get; } = "/setmail";
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Неверное количество аргументов, формат /setmail nickname");
                return;
            }

            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                await DBHelper.SetMailAsync(user.Id.ToString(), words[1]);
                
                await botClient.SendTextMessageAsync(user.ChatId,
                    $"Вам назначена новая почта {words[1]}");
            }
            catch (ConditionalCheckFailedException e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Некорректное id");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Произошла ошибка {e.Message}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}