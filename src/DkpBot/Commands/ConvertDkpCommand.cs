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
    public class ConvertDkpCommand : Command
    {
        public override string Name { get; } = "/convertdkp";

        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            var words = message.Text.Split(' ');
            var sw = new Stopwatch();
            sw.Start();
            if (words.Length < 2)
            {
                await botClient.SendTextMessageAsync(chatId, $"Некорректный формат команды, используйте: /convertdkp adenaCount");
                return;
            }
            
            var userName = "'not found'";
            
            try 
            {
                var adena = int.Parse(words[1]);
                var userResultAsync = await DBHelper.GetUserResultAsync(message.From.Id.ToString());
                var user = User.FromDict(userResultAsync.Item);
                userName = user.Name;
                var result = await DBHelper.ConvertDkpToAdena(adena);
                var users = result.Item1;
                var coef = result.Item2;
                await botClient.SendTextMessageAsync(Constants.AdminChatId,$"Всего набрали {result.totalDkp} дкп, коэффициент {coef}");
                var hCount = 0;
                foreach (var u in users)
                {
                    if (u.Dkp == 0)
                        continue;
                    var convertResult = await DBHelper.ConvertDkpToAdenaForUser(u, coef);
                    try
                    {
                        await botClient.SendTextMessageAsync(u.ChatId,$"Вам начислено {convertResult} миллионов адены за {u.Dkp} дкп");
                    }
                    catch (Exception e)
                    {
                        await botClient.SendTextMessageAsync(Constants.AdminChatId,$"ошибка {e} при {u.Name}");    
                    }
                    hCount++;
                }
                await botClient.SendTextMessageAsync(Constants.AdminChatId,$"Начисление завершено успешно, обработали {hCount} человек");
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}, обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {userName}, {e}");
                LambdaLogger.Log("ERROR: " + e);
            }

        }

        public override bool Match(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}