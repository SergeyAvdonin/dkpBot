using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public abstract class Command
    {
        public abstract string Name { get; }

        public abstract Task Execute(Message message, TelegramBotClient botClient);
        
        public abstract bool Match(Message message);
    }
}