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
    public class PvpDropCommand : Command
    {
        public override string Name { get; } = "/pvpdrop";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "'not found'";
            try 
            {
                var users = await DBHelper.GetAllUsers();
                var words = message.Text.Split(' ');
                if (words.Length < 2 || !int.TryParse(words[1], out var itemCount))
                {
                    await botClient.SendTextMessageAsync(chatId, $"Неправильный формат команды, введите кол-во предметов для раздачи");
                    return;
                }
                
                var msg = ""; 
                
                await botClient.SendTextMessageAsync(chatId, users.Count() + " колво");
                
                users = users.OrderByDescending(x => x.Adena).ToArray();
                var rawgroups = users.GroupBy(x => x.PartyLeader).OrderByDescending(x=>x.Count()).ToArray();
                var groups = rawgroups.Where(x => !string.IsNullOrEmpty(x.Key))
                    .Concat(rawgroups.Where(x => string.IsNullOrEmpty(x.Key))).ToArray();
                var pls = new HashSet<long>();
                var groupsWithPl = new Dictionary<string, List<User>>();
                foreach (var g in groups)
                {
                    var group = g.ToList();
                    if (g.Any(x=>x.Id.ToString() == g.Key))
                    {
                        //пл уже внутри
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(g.Key))
                        {
                            var pl = users.FirstOrDefault(x => x.Id.ToString() == g.Key);
                            if (pl != null)
                            {
                                group.Insert(0, pl);
                                pls.Add(pl.Id);
                            }
                        }
                    }

                    if (g.Key != null)
                        groupsWithPl[g.Key] = group;
                    else
                    {
                        groupsWithPl[""] = group.Where(x=> !pls.Contains(x.Id)).ToList();
                    }
                }

                var fullGroups = groupsWithPl.Where(x => x.Value.Count() > 2 
                                                         && !string.IsNullOrEmpty(x.Key))
                    .ToArray();
                
                var smallGroups = groupsWithPl
                    .Where(x => x.Value.Count() == 2)
                    .ToArray();
                
                msg += $"{fullGroups.Count()} пати из более двух человек\n";
                msg += $"{smallGroups.Count()} пати из двух человек, участники: \n";
                
                foreach (var g in smallGroups)
                {
                    foreach (var user in g.Value)
                    {
                        if (user.Id.ToString() == g.Key)
                            msg += "ПЛ: ";
                        msg += $"{user.Name}\n";
                    }
                    msg += $"------\n";
                }
                
                var solo = groupsWithPl
                    .Where(x => x.Value.Count() == 1 || string.IsNullOrEmpty(x.Key))
                    .SelectMany(x=>x.Value)
                    .Where(x=>!pls.Contains(x.Id))
                    .ToArray();
                
                msg += $"{solo.Count()} солоигроков, либо непривязанных: \n";
                foreach (var g in solo)
                {
                    msg += $"{g.Name} : {g.PvpPoints} пвп-очков\n";
                }
                
                var plToPoints = fullGroups
                    .ToDictionary(g => g.Key,
                        g => g.Value.Sum(u => u.PvpPoints));

                var totalPoints = plToPoints.Sum(x => x.Value);
                msg += $"Всего набрали {totalPoints} пвп-очков в паках\n";
                
                msg += $"Пвп поинтов и предметов на пак\n";
                foreach (var plToPoint in plToPoints)
                {
                    var share = (double) plToPoint.Value / totalPoints * itemCount;
                    msg += $"{users.FirstOrDefault(x=>x.Id.ToString() == plToPoint.Key)?.Name}: {plToPoint.Value} очков," +
                           $" получает {share:#.00} предметов, почта: {users.FirstOrDefault(x=>x.Id.ToString() == plToPoint.Key)?.Mail}\n";
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