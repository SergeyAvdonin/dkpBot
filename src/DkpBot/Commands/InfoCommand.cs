using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class InfoCommand : Command
    {
        public override string Name { get; } = "/info";
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var userName = "'not found'";
            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                if (words.Length > 1 && (int)user.Role <= 2)
                {
                    var target = await DBHelper.GetUserResultByNameAsync(words[1]);
                    if (target == null || !target.Any())
                    {
                        await botClient.SendTextMessageAsync(chatId, $"несуществующее имя: {words[1]}");
                        return;
                    }
                    var trgRes = await DBHelper.GetUserResultAsync(target["Id"].S);
                    var trg = User.FromDict(trgRes.Item);
                    await botClient.SendTextMessageAsync(chatId, trg.GetInfo());
                    
                }
                else
                {
                    userName = user.Name;
                    await botClient.SendTextMessageAsync(chatId, user.GetInfo());
                }
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}. Обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {userName}, {e}");
            }
        }
    }
}