using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using DkpBot.Commands;
using Telegram.Bot;

namespace DkpBot
{
    public static class Bot
    {
        private static TelegramBotClient _botClient;
        private const string ApiKey = "1637138385:AAHQfYvuG4J_0uJPqyillbESQBsBr1y6w0I";
        private static List<Command> _commandsList;
        
        public static readonly Dictionary<string, Role> CommandToMaxRole = new Dictionary<string, Role>
        {
            ["/addhero"] = Role.User,
            ["/deletehero"] = Role.User,
            ["/sharehero"] = Role.User,
            ["/register"] = Role.NotRegistered,
            ["/commands"] = Role.NotRegistered,
            ["/start"] = Role.NotRegistered,
            ["/changerole"] = Role.Admin,
            ["/info"] = Role.WaitingForAuthentication,
            ["/event"] = Role.User,
            ["/join"] = Role.User,
            ["/warmup"] = Role.Admin,
            ["/setpl"] = Role.User,
            ["/moveadena"] = Role.User,
            ["/top"] = Role.User,
            ["/convertdkp"] = Role.Admin,
            ["/changeevent"] = Role.User,
            ["/changecoeff"] = Role.SubAdmin,
            ["/addadena"] = Role.RaidLeader,
            ["/eventstats"] = Role.Admin,
            ["/userstats"] = Role.Admin,
            ["/adddkp"] = Role.RaidLeader,
            ["/viewevent"] = Role.User,
            ["/massremoveadena"] = Role.Admin,
            ["/setmail"] = Role.User,
            ["/rename"] = Role.User,
            ["/deleteoldevents"] = Role.Admin,
            ["/cleanstats"] = Role.Admin,
            ["/cleantotalstats"] = Role.Admin,
            ["/deleteevent"] = Role.Admin,
            ["/pvpdrop"] = Role.Admin,
            ["/customstats"] = Role.Admin,
            ["/customstats2"] = Role.Admin,
            ["/cleanadenadkp"] = Role.Admin,
            ["/cleancoeffs"] = Role.Admin,
        };
        
        public static IReadOnlyList<Command> Commands => _commandsList.AsReadOnly();
        
        public static async Task<TelegramBotClient> GetClient()
        {    
            if (_botClient != null)
            {
                return _botClient;
            }
            var sw = new Stopwatch();
            sw.Start();
            DBHelper.Init();
            LambdaLogger.Log("INFO: " + $"Creating database elapsed {sw.ElapsedMilliseconds}");
            _botClient = new TelegramBotClient(ApiKey);
            LambdaLogger.Log("INFO: " + $"+Creating bot elapsed {sw.ElapsedMilliseconds}");
            _commandsList = new List<Command>
            {
                new RegisterUserCommand(),
                new AddHeroCommand(),
                new GetCommands(),
                new ChangeRoleCommand(),
                new InfoCommand(),
                new ShareHeroCommand(),
                new DeleteHeroCommand(),
                new CreateEventCommand(),
                new JoinEventCommand(),
                new StartCommand(),
                new Warmup(),
                new SetPartyLeaderCommand(),
                new MoveAdenaCommand(),
                new TopCommand(),
                new ConvertDkpCommand(),
                new ChangeEventCommand(),
                new ChangeHeroCoefficientCommand(),
                new AddAdenaCommand(),
                new GetEventStatsCommand(),
                new GetUserStatsCommand(),
                new AddDkpCommand(),
                new RemoveAdenaMassCommand(),
                new ViewEventCommand(),
                new SetMailCommand(),
                new RenameHeroCommand(),
                new DeleteOldEvents(),
                new CleanStatsCommand(),
                new CleanTotalStatsCommand(),
                new DeleteEventCommand(),
                new PvpDropCommand(),
                new GetCustomStats(),
                new GetCustomStats2(),
                new CleanAdenaAndDkpCommand(),
                new CleanCoeffsCommand()
            };
            _commandsList.Add(new UnknownCommand());
            return _botClient;
        }
    }
}