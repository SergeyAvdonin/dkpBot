using System.Collections.Generic;
using System.Threading.Tasks;
using DkpBot.Commands;
using Telegram.Bot;

namespace DkpBot
{
    public static class Bot
    {
        private static TelegramBotClient _botClient;
        private const string ApiKey = "1637138385:AAHQfYvuG4J_0uJPqyillbESQBsBr1y6w0I";
        private const string HookUrl = "hookurl";
        private static List<Command> _commandsList;
        
        public static IReadOnlyList<Command> Commands => _commandsList.AsReadOnly();
        
        public static async Task<TelegramBotClient> GetClient()
        {
            if (_botClient != null)
            {
                return _botClient;
            }
            DBHelper.Init();
            _botClient = new TelegramBotClient(ApiKey);
            _commandsList = new List<Command>
            {
                new RegisterUserCommand(),
                new AddHeroCommand(),
                new GetCommands(),
                new ChangeRoleCommand(),
                new InfoCommand(),
                new ShareHeroCommand()
            };
            _commandsList.Add(new UnknownCommand());
            return _botClient;
        }
    }
}