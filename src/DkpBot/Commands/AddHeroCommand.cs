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
        readonly Regex nicknameRegex = new Regex("^[а-яА-ЯёЁa-zA-Z]+$");
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length < 2 || !nicknameRegex.IsMatch(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неправильно введено имя персонажа, регистрация не прошла, формат команды: /addhero heroName");
                return;
            }

            try
            {
                var result = await DBHelper.TryAddHero(words[1], message.From.Id, message.From.Username);
                if (result != null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Персонаж {words[1]} уже зарегистрирован. Владелец - {result}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы успешно зарегистрировали персонажа {words[1]}. Удалить персонажа - /deletehero heroName");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Добавлен персонаж {words[1]} для {message.From.Username}");
                }
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {message.From.Username}, {e}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

        public override bool Match(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.TextMessage)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}