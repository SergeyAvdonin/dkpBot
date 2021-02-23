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
                    $"\n\nПосле получения авторизации вы можете:\n" +
                    $"\t\tСоздать персонажа '/addhero имяПерсонажа.'\n" +
                    $"\t\tСоздать событие '/event имяСобытия кол-воЧеловек'\n" +
                    $"\t\tЗарегистрироваться на событие по коду '/join AAAAA имяПерсонажа.'\n" +
                    $"\t\tПросмотреть информацию о персонажа, накопленной адене и дкп '/info'\n" +
                    $"\t\tПоделиться персонажем с другом `/sharehero имяПерсонажа idДругогоЧеловека`.\n\n" +
                    $"Регистрация на событие заканчивается через 10 минут после создания. \nДкп поинты преобразуются в адену раз в неделю.\n" +
                    $"За адену можно будет покупать предметы на аукционе.";
                await botClient.SendTextMessageAsync(chatId, msg);
            }
            catch (Exception e)
            {
                await botClient.SendTextMessageAsync(chatId,  $"Произошла ошибка {e.Message}. Обратитесь к администратору");
                await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Ошибка у {message.From.FirstName}, {e}");
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