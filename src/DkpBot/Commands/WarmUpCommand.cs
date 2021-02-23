using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal.Util;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class Warmup : Command
    {
        public override string Name { get; } = "/warmup";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            LambdaLogger.Log("warming up...");
            await DBHelper.GetUserResultAsync(message.From.Id.ToString());
        }

        public override bool Match(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}