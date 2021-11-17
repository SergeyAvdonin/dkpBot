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

        readonly Regex nicknameRegex = new Regex("^[а-яА-ЯёЁa-zA-Z0-9]+$");
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var userName = message.From.Username;
            if (string.IsNullOrEmpty(userName))
                userName = message.From.FirstName;
            if (string.IsNullOrEmpty(userName))
                userName = "no_name";
            
            var words = message.Text.Split(' ');
            if (words.Length < 2 || string.IsNullOrEmpty(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неверный формат команды, правильный формат: /register Name");
                return;
            }
            
            var user = new User()
            {
                Active = true,
                Dkp = 0,
                WorldDkp = 0,
                Id = message.From.Id,
                ChatId = chatId,
                Role = Role.WaitingForAuthentication,
                TgLogin = userName,
                CreationDateTime = DateTime.UtcNow,
                Characters = new List<string>(),
                Name = words[1]
            };

            try
            {
                var result = await DBHelper.RegisterUser(user);
                if (result)
                    await botClient.SendTextMessageAsync(chatId, "Вы успешно зарегистрированы, ожидайте активацию. Посмотреть информацию по аккаунту - команда /info");
                else
                    await botClient.SendTextMessageAsync(chatId, "Вы уже зарегистрированы. Посмотреть информацию по аккаунту - команда /info");
                
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Попытка регистрации от {userName} ({message.From.Id} имя: {words[1]}), успех: {result}");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId, $"Неизвестная ошибка: {e}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у tgLogin={userName}, name={words[1]}, {e}");
            }
            
        }

    }
}