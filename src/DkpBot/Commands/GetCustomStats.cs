using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class GetCustomStats : Command
    {
        public override string Name { get; } = "/customstats";

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
                var events = await DBHelper.GetAllEveningPvpEventsAsync(int.Parse(words[1]));

                var msg = "";
                await botClient.SendTextMessageAsync(chatId, events.Count() + " колво ивентов с пвп-очками");

                var eveningEvents = events.Where(e =>
                    (DateTime.UnixEpoch + TimeSpan.FromSeconds(e.creationDateTimeSeconds)).Hour > 17 || (DateTime.UnixEpoch + TimeSpan.FromSeconds(e.creationDateTimeSeconds)).Hour < 1).ToArray();
                await botClient.SendTextMessageAsync(chatId, eveningEvents.Count() + " колво вечерних ивентов");

                var userToPoints = new Dictionary<string, int>();

                foreach (var eveningEvent in eveningEvents)
                {
                    foreach (var user in eveningEvent.participants)
                    {
                        if (!userToPoints.ContainsKey(user))
                            userToPoints.Add(user, 1);
                        else
                        {
                            userToPoints[user]++;
                        }
                    }
                }

                foreach (var u in userToPoints.OrderByDescending(x=>x.Value))
                {
                    msg += $"{u.Key}:{u.Value}\n";
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