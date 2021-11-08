using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class AddDkpCommand : Command
    {
        public override string Name { get; } = "/adddkp";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {

            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 4 || !int.TryParse(words[2], out var dkp))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /adddkp userName dkp reason");
                return;
            }

            var targetUserName = words[1];
            var userName = "'not found'";
            var reason = words[3];
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultByNameAsync(targetUserName);
                if (userResultAsync == null || !userResultAsync.Any())
                {
                    await botClient.SendTextMessageAsync(chatId, $"несуществующее имя: {targetUserName}");
                    return;
                }
                
                await DBHelper.AddDkpAsync(userResultAsync["Id"].S, dkp);

                if (dkp > 0)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы увеличили dkp на {dkp} пользователю {targetUserName}");
                    await botClient.SendTextMessageAsync(userResultAsync["ChatId"].N, $"Вам увеличили дкп на {dkp}, причина: {reason}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} увеличил дкп {targetUserName} на {dkp}, причина: {reason}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы уменьшили dkp на {-dkp} пользователю {targetUserName}");
                    await botClient.SendTextMessageAsync(userResultAsync["ChatId"].N, $"Вам уменьшили дкп на {-dkp}, причина: {reason}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} уменьшил дкп {targetUserName} на {-dkp}, причина: {reason}");
                }

            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Произошла ошибка у {userName} {e.Message}");
            }

        }

    }
}