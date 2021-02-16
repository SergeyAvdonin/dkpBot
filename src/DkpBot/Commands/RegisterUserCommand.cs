using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class RegisterUserCommand : Command
    {
        public override string Name { get; } = @"/register";

        readonly Regex nicknameRegex = new Regex("^[а-яА-ЯёЁa-zA-Z]+$");
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            //var words = message.Text.Split(' ');
            /*if (words.Length < 1 || !nicknameRegex.IsMatch(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неправильно введено имя персонажа, регистрация не прошла, формат команды: /register nickName");
                return;
            }*/
            var user = new User()
            {
                Active = true,
                Adena = 0,
                Dkp = 0,
                Id = message.From.Id,
                ChatId = chatId,
                Role = Role.WaitingForAuthentication,
                TgLogin = message.From.Username,
                CreationDateTime = DateTime.UtcNow,
                Characters = new List<string>(),
            };

            try
            {
                var result = await DBHelper.RegisterUser(user);
                if (result)
                    await botClient.SendTextMessageAsync(chatId, "Вы успешно зарегистрированы, ожидайте активацию. Посмотреть информацию по аккаунту - команда /info");
                else
                    await botClient.SendTextMessageAsync(chatId, "Вы уже зарегистрированы. Посмотреть информацию по аккаунту - команда /info");
                
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Попытка регистрации от {message.From.Username} ({message.From.Id}), успех: {result}");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId, $"Неизвестная ошибка: {e}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {message.From.Username}, {e}");
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