using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class ViewConstPartyCommand : Command
    {
        public override string Name { get; } = "/cpview";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chatId = message.Chat.Id;

            
            var userName = "?";
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                if (string.IsNullOrEmpty(user.ConstParty))
                {
                    await botClient.SendTextMessageAsync(chatId, "У вас нет кп");
                    return;
                }
                
                var cp = await DBHelper.GetCp(user.ConstParty);
                
                await botClient.SendTextMessageAsync(chatId, cp.GetInfo());
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {userName}, {e}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}