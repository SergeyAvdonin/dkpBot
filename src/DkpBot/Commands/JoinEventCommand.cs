using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class JoinEventCommand : Command
    {
        public override string Name { get; } = "/join";
        
        public enum JoinEventResult
        {
            Success,
            NoEvent,
            RegistrationClosed,
            TooManyPeople,
            NoAccessToHero,
            HeroDuplicate,
            UserDuplicate
        }
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length < 2  || string.IsNullOrEmpty(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /join code или /join code heroName");
                return;
            }

            var rawCode = words[1];
            
            var code = rawCode.ToUpper();
           
            var userName = "'not found'";
            
            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;

                if (user.Characters.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chatId, $"У вас 0 персонажей. Сначала нужно добавить персонажа командой /addhero heroName.");
                    return;
                }

                var heroName = words.Length > 2 ? words[2].ToLower() : user.Characters.Count == 1 ? user.Characters.Single() : null;
                
                if (heroName == null)    
                {
                    await botClient.SendTextMessageAsync(chatId, $"У вас больше одного персонажа, поэтому воспользуйтесь командой /join code heroName");
                    return;
                }

                var name = await DBHelper.GetEventNameAsync(code);

                if (name == null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Событие с кодом {rawCode} не найдено");
                    return;
                }
                var rusName = CreateEventCommand.EnRuEventNames.ContainsKey(name) 
                    ? CreateEventCommand.EnRuEventNames[name] 
                    : name;
                
                var world = (rusName == "хб" || rusName == "лоа" || rusName == "аден" || rusName == "лед" ||
                             rusName == "антарас");
                var result = await DBHelper.TryJoinEventAsync(code, heroName, message.From.Id, world);
                if (result.Item1 == JoinEventResult.NoEvent)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Событие с кодом {rawCode} не найдено");
                }
                if (result.Item1 == JoinEventResult.HeroDuplicate)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Персонаж {heroName} уже зарегистрирован");
                }
                if (result.Item1 == JoinEventResult.UserDuplicate)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Вы уже зарегистрированы на это событие");
                }
                if (result.Item1 == JoinEventResult.RegistrationClosed)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Регистрация на событие с кодом {rawCode} завершена {result.Item2} минут назад");
                }
                if (result.Item1 == JoinEventResult.TooManyPeople)
                {
                    await botClient.SendTextMessageAsync(chatId, $"На событие {rawCode} уже зарегистрировано максимум людей ({result.Item2})");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Избыточная регистрация на событие {rawCode} от {userName}");
                }
                if (result.Item1 == JoinEventResult.NoAccessToHero)
                {
                    await botClient.SendTextMessageAsync(chatId, $"У вас нет доступа к персонажу {heroName}");
                }
                if (result.Item1 == JoinEventResult.Success)
                {
                    var msg =
                        $"Успешная регистрация героя {heroName} на событие {result.Item3} код {rawCode}. Начислено {result.Item2} дкп";
                    if (result.Item4 > 0)
                        msg += $", {result.Item4} pvp-поинтов";
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