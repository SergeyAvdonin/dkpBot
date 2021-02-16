using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot.Commands
{
    public class GetCommands : Command
    {
        public static readonly Dictionary<string, Role> CommandToMaxRole = new Dictionary<string, Role>
        {
            ["/addhero"] = Role.User,
            ["/deletehero"] = Role.User,
            ["/sharehero"] = Role.User,
            ["/register"] = Role.NotRegistered,
            ["/commands"] = Role.NotRegistered,
            ["/help"] = Role.NotRegistered,
            ["/changerole"] = Role.Admin,
            ["/info"] = Role.WaitingForAuthentication,
        };
        
        public override string Name { get; } = "/commands";
        public override async Task Execute(Message message, TelegramBotClient botClient)
        {
            var role = await DBHelper.GetRole(message.From.Id);

            var available = CommandToMaxRole.Where(x => x.Value >= role);

            var myMessage = "Список доступных команд:\n" + string.Join("\n", available.Select(x => x.Key));

            await botClient.SendTextMessageAsync(message.Chat.Id, myMessage);
        }

        public override bool Match(Message message)
        {
            if (message.Type != Telegram.Bot.Types.Enums.MessageType.TextMessage)
                return false;

            return message.Text.Contains(this.Name);
        }
    }
}