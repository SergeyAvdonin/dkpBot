using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class GetCustomStats2 : Command
    {
        public override string Name { get; } = "/customstats2";

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

                var msg = "";
                await botClient.SendTextMessageAsync(chatId, events.Count() + " колво ивентов");
                

                var userToEvent = new Dictionary<string, int>();
                var userToPvp = new Dictionary<string, int>();

                foreach (var e in events)
                {
                    foreach (var user in e.participants)
                    {
                        if (!userToEvent.ContainsKey(user))
                            userToEvent.Add(user, 1);
                        else
                        {
                            userToEvent[user]++;
                        }
                    }
                }
                
                
                foreach (var e in events)
                {
                    foreach (var user in e.participants)
                    {
                        if (!userToPvp.ContainsKey(user))
                            userToPvp.Add(user, e.pvpPoints);
                        else
                        {
                            userToPvp[user]+=e.pvpPoints;
                        }
                    }
                }
                await botClient.SendTextMessageAsync(chatId, "герой:кол-во событий");
                foreach (var u in userToEvent.OrderByDescending(x=>x.Value))
                {
                    msg += $"{u.Key}:{u.Value}\n";
                }

                await botClient.SendTextMessageAsync(chatId, msg);
                await botClient.SendTextMessageAsync(chatId, "герой:кол-во пвп-поинтов");
                msg = "";
                foreach (var u in userToPvp.OrderByDescending(x=>x.Value))
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