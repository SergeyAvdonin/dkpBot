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
    public class AddAdenaCommand : Command
    {
        public override string Name { get; } = "/addadena";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 4 || !int.TryParse(words[2], out var adena))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /addadena id adena reason");
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
                var userResultFromAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultFromAsync.Item);
                userName = user.Name;
                
                await DBHelper.AddAdenaAsync(userResultAsync["Id"].S, adena);
                
                if (adena > 0)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы добавили адены {adena}кк у {targetUserName}");
                    await botClient.SendTextMessageAsync(userResultAsync["ChatId"].N, $"Вам добавили {adena}кк адены, причина: {reason}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} увеличил адену {targetUserName} на {adena}кк, причина: {reason}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы отняли адены {-adena}кк у {targetUserName}");
                    await botClient.SendTextMessageAsync(userResultAsync["ChatId"].N, $"Вам отняли {-adena}кк адены, причина: {reason}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} уменьшил адену у {targetUserName} на {-adena}кк, причина: {reason}");
                }
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}