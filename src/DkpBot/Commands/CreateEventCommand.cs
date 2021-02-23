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
            {"ак", 10},
            {"орфен", 10},
            {"ядро", 10},
            {"закен", 10},
            {"баюм", 20},
            {"тои", 15},
            {"боссы", 10},
            {"лоа", 20},
            {"лилит", 10},
            {"анаким", 10},
            {"осада", 20},
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
        };
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            bool withSelf = !message.Text.Contains("nojoin");
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ');
            var sw = new Stopwatch();
            sw.Start();
            if (words.Length < 3 || !int.TryParse(words[2], out var peopleC) || peopleC >= 1000 
                || (!eventNames.ContainsKey(words[1].ToLower()) && !enRuEventNames.ContainsKey(words[1].ToLower())))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /event eventName peopleCount\n" +
                                                             $"Возможные eventName:\n\t\t{string.Join("\n\t\t",enRuEventNames.Select(x => $"{x.Key} или {x.Value}"))}" +
                                                             $"PeopleCount < 1000");
                return;
            }

            var eventName = words[1].ToLower();
            var userName = "'not found'";
            
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                LambdaLogger.Log("user create: " + sw.ElapsedMilliseconds);    
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
            
                var code = await DBHelper.TryCreateEventAsync(words[1], words[2], points, message.From.Id);
                await botClient.SendTextMessageAsync(chatId,$"Вы успешно cоздали событие {words[1]} на {words[2]} человек. КОД: {code}");
                LambdaLogger.Log("+event created: " + sw.ElapsedMilliseconds);    
                if (user.Characters.Count == 1 && withSelf)
                {
                    var heroName = user.Characters.Single();
                    var (_, dkp) = await DBHelper.TryJoinEventAsync(code, heroName, message.From.Id);
                    await botClient.SendTextMessageAsync(chatId, $"Успешная регистрация героя {heroName} на событие {code}. Начислено {dkp} дкп");
                }
                LambdaLogger.Log("+character joined: " + sw.ElapsedMilliseconds);    
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {userName}, {e}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

        public override bool Match(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}