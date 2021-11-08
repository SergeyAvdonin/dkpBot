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
    public class GetEventStatsCommand : Command
    {
        public override string Name { get; } = "/eventstats";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {

            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /eventstats days");
                return;
            }

            var userName = "'not found'";
            try 
            {
                var events = await DBHelper.GetEventsAsync(int.Parse(words[1]));

                var msg = ""; /*$@"Подозрительные события\n";
                foreach (var e in events)
                {
                    if (e.participants.Length < 2)
                    {
                       msg+=$"{e}\n";
                    }
                }
                */

                msg += "РЛЫ:\n";
                var groups = events.GroupBy(x => x.raidLeaderId).OrderByDescending(x=>x.Count());
                foreach (var g in groups)
                {
                    var user = User.FromDict((await DBHelper.GetUserResultAsync(g.Key.ToString())).Item);
                    msg += $"{g.Key} : событий - {g.Count()}, процент от всех - {(double)g.Count() / events.Length * 100}%, имя - {user.Name}\n";
                }

                await botClient.SendTextMessageAsync(chatId, msg);
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e}, обратитесь к администратору");
                
                LambdaLogger.Log("ERROR: " + e);
            }

        }
    }
}