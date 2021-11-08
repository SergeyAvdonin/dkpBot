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
    public class RemoveAdenaMassCommand : Command
    {
        public override string Name { get; } = "/massremoveadena";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(new[]{' ', '\n', '\t'}, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length < 2 || words.Length % 2 != 1)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды");
                return;
            }

            var userName = "'not found'";
            try 
            {
                var namesCount = (words.Length - 1) / 2;
                var pairs = new (string, double)[namesCount];
                for (int i = 0; i < namesCount; i++)
                {
                    if (double.TryParse(words[i + namesCount + 1], out var num))
                        pairs[i] = (words[i + 1], num);
                    else
                    {
                        await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Не удалось определить кол-во снятой адены с {words[i+1]}");
                        return;
                    }
                }

                for (int i = 0; i < namesCount; i++)
                {
                    if (pairs[i].Item2 < 0.5)
                        continue;
                    
                    var userResultAsync = await DBHelper.GetUserResultByNameAsync(pairs[i].Item1);
                    if (userResultAsync == null || !userResultAsync.Any())
                    {
                        await botClient.SendTextMessageAsync(chatId, $"несуществующее имя: {pairs[i].Item1}");
                        continue;
                    }
                    await DBHelper.AddAdenaAsync(userResultAsync["Id"].S, -(int)pairs[i].Item2);
                    await botClient.SendTextMessageAsync(Constants.AdminChatId, $"отняли адены у {pairs[i].Item1} кол-во: {(int)pairs[i].Item2}");
                }
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message} у {userName}, обратитесь к администратору");
                LambdaLogger.Log("ERROR: " + e);
            }
        }
    }
}