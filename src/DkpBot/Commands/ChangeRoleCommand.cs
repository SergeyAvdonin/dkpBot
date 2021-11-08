using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class ChangeRoleCommand : Command
    {
        public override string Name { get; } = "/changerole";
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length != 3 || !int.TryParse(words[2], out _))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неверное количество аргументов");
                return;
            }

            try
            {
                var targetChatId = await DBHelper.ChangeRoleAsync(words[1], words[2]);
                await botClient.SendTextMessageAsync(targetChatId,
                    $"Вам назначена новая роль {(Role) int.Parse(words[2])}");
                await botClient.SendTextMessageAsync(chatId,
                    $"Успех. Пользователю с id = {words[1]} назначена роль {(Role) int.Parse(words[2])}");
            }
            catch (ConditionalCheckFailedException e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Некорректное id");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}