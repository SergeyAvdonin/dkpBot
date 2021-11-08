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
    public class GetUserStatsCommand : Command
    {
        public override string Name { get; } = "/userstats";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
    
            var chatId = message.Chat.Id;

            var userName = "'not found'";
            try 
            {
                var users = await DBHelper.GetAllUsers();

                var msg = ""; /*$@"Подозрительные события\n";
                foreach (var e in events)
                {
                    if (e.participants.Length < 2)
                    {
                       msg+=$"{e}\n";
                    }
                }
                */
                await botClient.SendTextMessageAsync(chatId, users.Count() + " колво");
                
                users = users.OrderByDescending(x => x.Adena).ToArray();
                var rawgroups = users.GroupBy(x => x.PartyLeader).OrderByDescending(x=>x.Count()).ToArray();
                var groups = rawgroups.Where(x => !string.IsNullOrEmpty(x.Key))
                    .Concat(rawgroups.Where(x => string.IsNullOrEmpty(x.Key)));
                var pls = new HashSet<long>();
                foreach (var g in groups)
                {
                    var group = g.ToList();

                    if (group.Any(x=>x.Id.ToString() == g.Key))
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
                                group.Insert(0,pl);
                                pls.Add(pl.Id);
                            }
                        }
                    }

                    foreach (var member in group)
                    {
                        var pl = "Отсутствует";
                        var mail = member.Mail;
                        if (string.IsNullOrEmpty(mail))
                            mail = member.Characters.FirstOrDefault();
                        if (!string.IsNullOrEmpty(g.Key))
                        {
                            var user = users.FirstOrDefault(x => x.Id.ToString() == g.Key);
                            if (user == null)
                            {
                                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Подозрительный чувак с id {g.Key}");
                            }
                            else
                                pl = user.Name;
                            
                            msg += $"{member.Name}\t{member.Adena}\t{mail}\t{pl}\n";
                        }
                        else
                        {
                            if (!pls.Contains(member.Id)) 
                                msg += $"{member.Name}\t{member.Adena}\t{mail}\t{pl}\n";
                        }
                    }
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