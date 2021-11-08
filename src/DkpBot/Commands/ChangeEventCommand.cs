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
    public class ChangeEventCommand : Command
    {
        public override string Name { get; } = "/changeevent";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {

            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ');
            if (words.Length < 3 || !int.TryParse(words[2], out var peopleCount) || peopleCount < 0 || string.IsNullOrEmpty(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /changeevent CODE peopleCount(неотрицательное)");
                return;
            }

            var eventCode = words[1];
            var userName = "'not found'";
            
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                await botClient.SendTextMessageAsync(chatId, $"CODE {eventCode}");
                var oldPeople = await DBHelper.ChangeEventPeopleCount(eventCode, peopleCount);
                if (oldPeople == -1)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Событие с таким кодом не найдено, верный формат:\n /changeevent CODE peopleCount dkpPoints");
                    return;
                }
                await botClient.SendTextMessageAsync(chatId, $"Изменено количество людей с {oldPeople} до {peopleCount}");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} изменил событие {eventCode} с {oldPeople} до {peopleCount} человек");
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