using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class StartCommand : Command
    {
        public override string Name { get; } = "/start";
        
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            
            try
            {
                var msg =
                    $"Добро пожаловать в дкп систему.\nРегистрируйтесь командой /register имя(в тс или игре) и ожидайте авторизацию.\nТекущий статус можно посмотреть командой /info" +
                    $"\n\nОбщие команды:\n" +
                    $"-Создать персонажа '/addhero имяПерсонажа.'\n" +
                    $"-Переименовать персонажа '/rename староеИмя новоеИмя.'\n" +
                    $"-Создать событие '/event имяСобытия кол-воЧеловек'\n" +
                    $"-Зарегистрироваться на событие по коду '/join AAAAA имяПерсонажа.'\n" +
                    $"-Просмотреть информацию о персонажа, накопленных дкп '/info'\n" +
                    $"-Поделиться персонажем с другом `/sharehero имяПерсонажа idДругогоЧеловека`.\n" +
                    $"-Посмотреть топ `/top`.\n\n" +
                    $"Команды для КП:\n\n" +
                    $"-Создать КП `/cpnew name`.\n" +
                    $"-Присоединиться к КП`/cpjoin name`.\n" +
                    $"-Выйти из КП`/cpleave`.\n" +
                    $"-Посмотреть информацию по своему КП `/cpview`.\n\n" +

                    $"Регистрация на событие заканчивается через 20 минут после создания.";
                await botClient.SendTextMessageAsync(chatId, msg);
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}. Обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {message.From.FirstName}, {e}");
            }
        }

    }
}