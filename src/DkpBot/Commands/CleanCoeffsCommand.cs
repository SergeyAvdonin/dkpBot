using System;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DkpBot.Commands
{
    public class CleanCoeffsCommand : Command
    {
        public override string Name { get; } = "/cleancoeffs";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "";
            try 
            {
                await DBHelper.CleanAllCoefsAsync();
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Обнулили коэфы");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

    }
}