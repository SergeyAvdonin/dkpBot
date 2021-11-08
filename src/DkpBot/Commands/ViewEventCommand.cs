using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DkpBot.Commands
{
    public class ViewEventCommand : Command
    {
        public override string Name { get; } = "/viewevent";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /viewevent id");
                return;
            }

            var userName = "";
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                var eventUsers = await DBHelper.GetEventUsersAsync(words[1]);
                if (eventUsers == null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"События с id {words[1]} не найдено");
                    return;
                }
                
                var sb = new StringBuilder();
                sb.AppendLine();
                for (int i = 0; i < eventUsers.Count; i++)
                {
                    sb.AppendLine($"{i+1}) {eventUsers[i]}");
                }
                
                await botClient.SendTextMessageAsync(chatId, $"```{sb}```", ParseMode.Markdown);
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}