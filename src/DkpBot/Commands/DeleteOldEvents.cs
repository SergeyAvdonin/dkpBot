using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class DeleteOldEvents : Command
    {
        public override string Name { get; } = "/deleteoldevents";
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            try
            {
                await DBHelper.DeleteEventsAsync(7);
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Success");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(Constants.AdminChatId,  $"Произошла ошибка {e.Message}");
                LambdaLogger.Log("ERROR: " + e);
            }
        }
    }
}