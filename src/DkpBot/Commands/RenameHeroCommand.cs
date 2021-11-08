using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public enum RenameResult
    {
        Success,
        NoAccess,
        NoAccessToNewName,
        NoHero
    }
    
    public class RenameHeroCommand : Command
    {
        public override string Name { get; } = "/rename";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length < 3)
            {
                await botClient.SendTextMessageAsync(chatId, $"Отсутствует имя персонажа: /rename oldName newName");
                return;
            }

            var userName = "'not found'";
            
            try
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                
                var result = await DBHelper.TryRenameHero(words[1].ToLower(), message.From.Id, words[2], userName);
                if (result == RenameResult.NoHero)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Персонажа {words[1]} не существует");
                }
                else if (result == RenameResult.NoAccess)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы не являетесь владельцем персонажа {words[1]}, поэтому не можете его переименовать.");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь с id = {message.From.Id} не смог переименовать персонажа {words[1]} (нет доступа)");
                }
                else if (result == RenameResult.NoAccessToNewName)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Имя {words[2]} занято. Можно давать имена с суффиксом в виде сервера, например, Белочка_Damask");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь с id = {message.From.Id} не смог переименовать персонажа {words[1]} в {words[2]} (имя занято)");
                }
                else if (result == RenameResult.Success)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы успешно переименовали персонажа {words[1]} в {words[2]}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь с id = {message.From.Id} переименовал персонажа {words[1]} в {words[2]}");
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