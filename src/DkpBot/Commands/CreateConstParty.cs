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
    public class CreateEventCommand : Command
    {
        public override string Name { get; } = "/event";
        const string AllowedChars = "0123456789";
        static Random rng = new Random();
        public static string GetCode(int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; ++i)
            {
                int element = rng.Next(0, AllowedChars.Length);
                chars[i] = AllowedChars.ElementAt(element);
            }
            return new string(chars);
        }
        
        private Dictionary<string, int> eventNames = new Dictionary<string, int>()
        {
            {"ак", 1},
            {"орфен", 1},
            {"ядро", 1},
            {"закен", 1},
            {"баюм", 1},
            {"тои", 1},
            {"боссы", 1},
            {"лоа", 1},
            {"лилит", 1},
            {"анаким", 1},
            {"осада", 1},
            {"пвп", 1},
            {"хб", 1},
            {"лед", 1},
        };

        private Dictionary<string, string> enRuEventNames = new Dictionary<string, string>()
        {
            {"aq", "ак"},
            {"orfen", "орфен"},
            {"core", "ядро"},
            {"zaken", "закен"},
            {"baum", "баюм"},
            {"toi", "тои"},
            {"boss", "боссы"},
            {"loa", "лоа"},
            {"lilith", "лилит"},
            {"anakim", "анаким"},
            {"siege", "осада"},
            {"pvp", "пвп"},
            {"hb", "хб"},
            {"led", "лед"},
        };
        
        private static Regex fightRegex = new Regex("f(\\d){0,1}");
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            bool withSelf = !message.Text.Contains("nojoin");
            var chatId = message.Chat.Id;


            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            

            if (words.Length < 3 || !int.TryParse(words[2], out var peopleC) || peopleC >= 1000 
                || (!eventNames.ContainsKey(words[1].ToLower()) && !enRuEventNames.ContainsKey(words[1].ToLower())))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /event eventName peopleCount (f)\n" +
                                                             $"Возможные eventName:\n\t\t{string.Join("\n\t\t",enRuEventNames.Select(x => $"{x.Key} или {x.Value}"))}" +
                                                             $"PeopleCount < 1000");
                return;
            }

            var eventName = words[1].ToLower();
            var userName = "'not found'";
            
            try 
            {
                var pvpPoints = 0;

                if (words.Length > 3)
                {
                    var word = words[3];
                    var match = fightRegex.Match(word);
                    if (match.Success)
                    {    
                        int.TryParse(match.Groups[0].Value.Replace("f", ""), out pvpPoints);
                    }
                }
                
                if (words.Length > 4)
                {
                    var word = words[4];
                    var match = fightRegex.Match(word);
                    if (match.Success)
                    {
                        int.TryParse(match.Groups[0].Value.Replace("f", ""), out pvpPoints);
                    }
                }
               
                
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                if (int.Parse(words[2]) <= 0)
                {
                    await botClient.SendTextMessageAsync(chatId,$"В событии должно участвовать не менее 1 человека");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Попытка создать событие на {int.Parse(words[2])} человек от {userName}");
                    return;
                }

                var rusName = enRuEventNames.ContainsKey(eventName) 
                    ? enRuEventNames[eventName] 
                    : eventName;

                var points = eventNames[rusName];
                if (words.Length > 3 && int.TryParse(words[3], out var customDkp))
                {
                    points = customDkp;
                }
            
                var code = await DBHelper.TryCreateEventAsync(words[1], words[2], points, message.From.Id, pvpPoints);
                await botClient.SendTextMessageAsync(chatId,$"Вы успешно cоздали событие {words[1]} на {words[2]} человек. КОД: {code}");
                await botClient.SendTextMessageAsync(Constants.AdminChatId,
                    $"Создано событие {words[1]} на {int.Parse(words[2])} человек от {userName}, dkp: ({points}) code {code}");
                if (user.Characters.Count == 1 && withSelf)
                {
                    var heroName = user.Characters.Single();
                    var (_, dkp, eName, pvp) = await DBHelper.TryJoinEventAsync(code, heroName, message.From.Id);
                    var msg = $"Успешная регистрация героя {heroName} на событие {eName}, {code}. Начислено {dkp} дкп";
                    if (pvp > 0)
                        msg += $", {pvp} pvp-поинтов";
                    await botClient.SendTextMessageAsync(chatId, msg);
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