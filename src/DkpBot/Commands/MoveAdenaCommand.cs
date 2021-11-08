using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class MoveAdenaCommand : Command
    {
        public enum MoveEventResult
        {
            Success,
            NotEnough,
            NoPL,
        }
        
        public override string Name { get; } = "/moveadena";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var userName = "'not found'";
            var chatId = message.Chat.Id;
            
            try
            {
                var words = message.Text.Split(' ');

                if (words.Length < 2)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Неправильный формат команды, отсутствует количество адены: /moveadena adena");
                    return;
                }

                if (!int.TryParse(words[1], out var adena) || adena < 0)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Неправильный формат команды, адена должна быть неотрицательным целым числом: /moveadena adena");
                    return;
                }
                
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                
                userName = user.Name;

                var result = await DBHelper.TryMoveAdena(user, adena);
                
                if (result == MoveEventResult.NoPL)
                {
                    await botClient.SendTextMessageAsync(chatId, $"У Вас не добавлен пати лидер, добавьте его с помощью команды /setpl id");
                }
                else if(result == MoveEventResult.NotEnough)
                {
                    await botClient.SendTextMessageAsync(chatId,$"У вас недостаточно адены.");
                }
                else if (result == MoveEventResult.Success)
                {
                    var plChatId = await DBHelper.GetChatIdAsync(user.PartyLeader);
                    await botClient.SendTextMessageAsync(chatId, $"Вы перевели пати лидеру с id={user.PartyLeader} {adena} адены");
                    await botClient.SendTextMessageAsync(plChatId, $"Пользователь {user.Name} перевел вам {adena} адены");
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Пользователь {user.Name} перевел пати лидеру id={user.PartyLeader} {adena} адены");
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