using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public enum ShareResult
    {
        Success,
        NoAccess,
        NoUser,
        AlreadyHas
    }
    
    public class ShareHeroCommand : Command
    {
        public override string Name { get; } = "/sharehero";
        readonly Regex nicknameRegex = new Regex("^[а-яА-ЯёЁa-zA-Z]+$");
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var words = message.Text.Split(' ');
            if (words.Length < 2 || !nicknameRegex.IsMatch(words[1]))
            {
                await botClient.SendTextMessageAsync(chatId, $"Неправильно введено имя персонажа, расшарка не прошла, формат команды: /sharehero heroName id");
                return;
            }
            
            if (words.Length < 3)
            {
                await botClient.SendTextMessageAsync(chatId, $"Не введен id аккаунта, на который расшариваем персонажа, формат команды: /sharehero heroName id");
                return;
            }
            try
            {
                var result = await DBHelper.TryShareHeroAsync(words[1], words[2], message.From.Id.ToString(), message.From.Username);
                if (result.Item1 == ShareResult.NoAccess)
                {
                    if (string.IsNullOrEmpty(result.Item2))
                        result.Item2 = "отсутствует";
                    await botClient.SendTextMessageAsync(chatId, $"У вас нет доступа к персонажу {words[1]}. Владелец - {result.Item2}");
                }
                else if (result.Item1 == ShareResult.AlreadyHas)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Персонаж {words[1]} уже был расшарен. Забрать персонажа - /removeshare heroName id");
                    
                }
                else if (result.Item1 == ShareResult.NoUser)
                {
                    await botClient.SendTextMessageAsync(chatId,$"Пользователь {words[2]} для передачи не найден. Введите корректный id");
                }
                else if (result.Item1 == ShareResult.Success)
                {
                    var targetUserChatId = await DBHelper.GetChatIdAsync(words[2]);
                    await botClient.SendTextMessageAsync(chatId,$"Персонаж {words[1]} успешно расшарен пользователю {words[2]}");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Расшарен персонаж {words[1]} от {message.From.Username} пользователю {words[2]}");
                    await botClient.SendTextMessageAsync(targetUserChatId, $"Вам расшарен персонаж {words[1]} от {message.From.Username}");
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