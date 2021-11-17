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
    public class CreateCpCommand : Command
    {
        public override string Name { get; } = "/newcp";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chatId = message.Chat.Id;

            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /newcp name");
                return;
            }

            var cpName = words[1];
            var userName = "?";
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                if (string.IsNullOrEmpty(user.ConstParty))
                {
                    await botClient.SendTextMessageAsync(chatId, "У вас уже есть кп");
                    return;
                }
                
                var result = await DBHelper.TryCreateCp(user.Id, cpName);
                
                if (result)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы успешно создали кп {cpName}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, $"Ошибка. Кп с именем '{cpName}' уже существует");
                }
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