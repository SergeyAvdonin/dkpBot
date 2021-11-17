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
    public class TopCpsCommand : Command
    {
        public override string Name { get; } = "/topcp";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;

            var userName = "'not found'";
            
            try
            {
                var text = message.Text;
                
                var words = text.Split(' ', '\n', '\t');

                var count = 50;
                if(words.Length > 1 && int.TryParse(words[1], out count)) {}
                
                if (count > 100 || count < 0)
                    count = 100;
                
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                
                userName = user.Name;
                
                var cps = await DBHelper.GetTopCpsAsync(count);
                var totalPoints = cps.Sum(x => x.TotalPoints);
                if (totalPoints == 0)
                    totalPoints = 1;
                var totalWorldPoints = cps.Sum(x => x.TotalWorldPoints);
                if (totalWorldPoints == 0)
                    totalWorldPoints = 1;
                var sep = new string(Enumerable.Repeat('-', 38).ToArray());
                await botClient.SendTextMessageAsync(chatId, "```\n" + sep + $"\n|{"Имя кп", -20}|{"Всего Dkp", -14}|{"Всего WorldDkp", -14}|\n" + sep + "\n" + string.Join("\n", cps.Select(x => $"|{x.Name, -20}|{x.TotalPoints + $" ({x.TotalPoints*100.0/totalPoints:0.#}%)", -14}|{x.TotalWorldPoints + $" ({x.TotalWorldPoints*100.0/totalWorldPoints:0.#}%)", -14}|")) + "\n" + sep + "```"
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