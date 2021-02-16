using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class UnknownCommand : Command
    {
        public override string Name { get; } = "";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            await botClient.SendTextMessageAsync(chatId, $"Такая команда не найдена ({message.Text}), используйте /commands для отображения списка доступных команд");
        }

        public override bool Match(Message message)
        {
            return true;
        }
    }
}