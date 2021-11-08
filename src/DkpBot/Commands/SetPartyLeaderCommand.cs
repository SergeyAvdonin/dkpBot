using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class SetPartyLeaderCommand : Command
    {
        public override string Name { get; } = "/setpl";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var userName = "'not found'";
            var chatId = message.Chat.Id;
            
            try
            {
                var words = message.Text.Split(' ');

                if (words.Length < 2)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Неправильный формат команды, отсутствует id пати лидера: /setpl id");
                    return;
                }

                var partyLeaderId = words[1];

                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;

                var result = await DBHelper.TryAddPartyLeader(user, partyLeaderId);
                
                if (result == null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Пользователь с id={partyLeaderId} не найден");
                }
                if (result == "cycleError")
                {
                    await botClient.SendTextMessageAsync(chatId, $"Пользватель с id={partyLeaderId} не может быть ПЛ-ом, так как у него есть свой ПЛ");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Не был добавлен пати лидер {result} для {userName} из-за цикличности");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId,$"Вы успешно добавили пати лидера {result}.");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Добавлен пати лидер {result} для {userName}");
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