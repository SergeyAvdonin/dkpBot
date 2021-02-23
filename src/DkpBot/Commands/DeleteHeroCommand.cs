using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public enum DeleteResult
    {
        Success,
        NoAccess,
        NoHero
    }
    
    public class DeleteHeroCommand : Command
    {
        public override string Name { get; } = "/deletehero";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Отсутствует имя персонажа: /deletehero heroName");
                return;
            }

            var userName = "'not found'";
            
            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                
                var result = await DBHelper.TryDeleteHero(words[1].ToLower(), message.From.Id);
                if (result == DeleteResult.NoHero)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Персонажа {words[1]} не существует");
                }
                else if (result == DeleteResult.NoAccess)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы не являетесь владельцем персонажа {words[1]}, поэтому не можете его удалить.");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь с id = {message.From.Id} не смог удалить персонажа {words[1]}");
                }
                else if (result == DeleteResult.Success)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы успешно удалили персонажа {words[1]}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь с id = {message.From.Id} удалил персонажа {words[1]}");
                }
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