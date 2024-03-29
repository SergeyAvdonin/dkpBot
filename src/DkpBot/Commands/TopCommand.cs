using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DkpBot.Commands
{
    public class TopCommand : Command
    {
        public override string Name { get; } = "/top";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "'not found'";
            
            try
            {
                var count = 50;

                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                
                userName = user.Name;
                
                var result = await DBHelper.GetTopByCharsAsync(count);
                var sep = new string(Enumerable.Repeat('-', 38).ToArray());
                await botClient.SendTextMessageAsync(chatId, "```\n" + sep + $"\n|{"Имя персонажа", -20}|{"Dkp", -4}|{"Пак", -12}|\n" + sep + "\n" + string.Join("\n", result.Select(x => $"|{x.character, -20}|{x.dkp, -4}|{x.cp, -12}|")) + "\n" + sep + "```"
                , ParseMode.Markdown);
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