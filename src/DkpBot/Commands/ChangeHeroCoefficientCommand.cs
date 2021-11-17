using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class ChangeHeroCoefficientCommand : Command
    {
        public override string Name { get; } = "/cf";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {

            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ');
            if (words.Length < 3 || !double.TryParse(words[2], out var coeff))
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте:\n /changeevent CODE peopleCount(неотрицательное)");
                return;
            }

            var heroName = words[1].ToLower();
            var userName = "'not found'";
            
            try 
            {
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;

                var owner = await DBHelper.ChangeHeroCoefficient(heroName, coeff);
                if (owner == null)
                {
                    await botClient.SendTextMessageAsync(chatId, $"персонаж {heroName} не найден");
                    return;
                }

                await botClient.SendTextMessageAsync(chatId, $"Вы сменили коэффициент у {heroName} на {coeff}");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"{userName} сменил коэффициент у {heroName} на {coeff}");
                await botClient.SendTextMessageAsync(owner.ChatId, $"У персонажа {heroName} сменился коэффициент на {coeff}");
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