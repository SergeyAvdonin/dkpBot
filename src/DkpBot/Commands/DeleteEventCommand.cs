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
    public class DeleteEventCommand : Command
    {
        public override string Name { get; } = "/deleteevent";

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
                var eventUsers = await DBHelper.GetEventUsersWithPointsAsync(words[1]);
                if (eventUsers == null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"События с id {words[1]} не найдено");
                    return;
                }

                foreach (var str in eventUsers)
                {
                    var chunks = str.Split("::");
                    await DBHelper.AddDkpAsync(chunks[0], -int.Parse(chunks[1]));
                    await botClient.SendTextMessageAsync(chatId, $"Удаляем дкп у {chunks[0]} кол-во {chunks[1]}");
                }

                await DBHelper.DeleteEventAsync(words[1]);
                await botClient.SendTextMessageAsync(chatId, $"Удаляем ивент {words[1]}");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}