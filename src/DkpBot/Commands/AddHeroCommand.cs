using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class AddHeroCommand : Command
    {
        public override string Name { get; } = "/addhero";
        private readonly Regex nicknameRegex = new Regex("^[а-яА-ЯёЁa-zA-Z0-9]+$");
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');

            if (words.Length < 2 || !nicknameRegex.IsMatch(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неправильно введено имя персонажа, регистрация не прошла, формат команды: /addhero heroName");
                return;
            }

            var userName = "'not found'";
            
            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                
                var result = await DBHelper.TryAddHero(words[1].ToLower(), message.From.Id, userName, words[1]);
                if (result != null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Персонаж {words[1]} уже зарегистрирован. Владелец - {result}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы успешно зарегистрировали персонажа {words[1]}. Удалить персонажа - /deletehero heroName");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Добавлен персонаж {words[1]} для {userName}");
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