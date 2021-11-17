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
    public class TopUsersCommand : Command
    {
        public override string Name { get; } = "/topusers";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "'not found'";
            
            try
            {
                var text = message.Text.Replace(" adena", "");
                
                var words = text.Split(' ', '\n', '\t');

                var count = 50;
                if(words.Length > 1 && int.TryParse(words[1], out count)) {}
                
                if (count > 100 || count < 0)
                    count = 100;
                
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                
                userName = user.Name;
                
                var result = await DBHelper.GetTopAsync(count);
                var sep = new string(Enumerable.Repeat('-', 38).ToArray());
                await botClient.SendTextMessageAsync(chatId, "```\n" + sep + $"\n|{"Имя", -20}|{"Dkp", -4}|{"WorldDkp", -8}|{"Пак", -12}|\n" + sep + "\n" + string.Join("\n", result.Select(x => $"|{x.Name, -20}|{x.Dkp, -4}|{x.WorldDkp, -8}|{x.ConstParty, -12}|")) + "\n" + sep + "```"
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