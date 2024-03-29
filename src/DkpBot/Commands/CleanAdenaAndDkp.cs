using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DkpBot.Commands
{
    public class CleanDkpCommand : Command
    {
        public override string Name { get; } = "/cleandkp";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "";
            try 
            {
                var users = await DBHelper.GetAllUsers();
                foreach (var u in users)
                {
                    await DBHelper.CleanAdenaAndDkpAsync(u);
                }
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Обнулили стату у {users.Length} человек");
                
                var heroesCount = await DBHelper.CleanAllHeroesDkp();
              
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Обнулили стату у {heroesCount} персонажей");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}