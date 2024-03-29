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
    public class JoinCpCommand : Command
    {
        public override string Name { get; } = "/cpjoin";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chatId = message.Chat.Id;

            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /cpjoin name");
                return;
            }

            var cpName = words[1];
            var userName = "?";
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                if (!string.IsNullOrEmpty(user.ConstParty))
                {
                    await botClient.SendTextMessageAsync(chatId, "У вас уже есть кп");
                    return;
                }

                var cp = await DBHelper.GetCp(cpName);
                if (cp == null)
                {
                    await botClient.SendTextMessageAsync(chatId, "кп не найдена");
                    return;
                }
                
                await DBHelper.JoinConstParty(user, cpName);
                
                await botClient.SendTextMessageAsync(chatId, $"Вы присоединились к кп {cpName}");
                await DBHelper.JoinConstParty(user, cpName);
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