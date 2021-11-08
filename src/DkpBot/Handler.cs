using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using DkpBot.Commands;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace DkpBot
{
    public class Handler
    {
        public string ApiKey = "1637138385:AAHQfYvuG4J_0uJPqyillbESQBsBr1y6w0I";
        public string address = "https://rz3278gpu2.execute-api.us-east-1.amazonaws.com/Prod/update/";
        
        //get
        public async Task<APIGatewayProxyResponse> Register( APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            await new TelegramBotClient(ApiKey).SetWebhookAsync(address);
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
            };
        }
        
        
        //post
        public async Task<APIGatewayProxyResponse> UpdateHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var sw = new Stopwatch();
            sw.Start();
            LambdaLogger.Log("CONTEXT: " + JsonConvert.SerializeObject(context));
            LambdaLogger.Log("EVENT: " + JsonConvert.SerializeObject(apigProxyEvent));
            LambdaLogger.Log("start deserialize");
            var msg = apigProxyEvent.Body;
            if (apigProxyEvent.Body == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                };
            }
            if (apigProxyEvent.Body.Contains("type\":\"phone_number\""))
            {
                LambdaLogger.Log("PHONE_TYPE ERROR");
                msg = apigProxyEvent.Body.Replace("phone_number", "bot_command");
                   
            }
            
            var update = JsonConvert.DeserializeObject<Update>(apigProxyEvent.Body);
            LambdaLogger.Log("finish deserialize");
            if (update == null || update.EditedMessage != null) 
                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                };

            var botClient = await Bot.GetClient();
            var commands = Bot.Commands;
            var message = update.Message;
            
            var role = await DBHelper.GetRole(message.From.Id);
            foreach (var command in commands)
            {
                if (command.Match(message))
                {
                    if (!(command is UnknownCommand) && Bot.CommandToMaxRole[command.Name] < role)
                    {
                        if(role == Role.NotRegistered)
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Вам недоступна эта команда, потому что вы не зарегистрированы, зарегистрируйтесь с помощью команды /register");
                        else
                            await botClient.SendTextMessageAsync(message.Chat.Id, $"Вам не доступна команда {command.Name}");
                    }
                    else
                    {
                        try
                        {
                            await command.Execute(message, botClient);
                        }
                        catch (Exception e)
                        {
                            await botClient.SendTextMessageAsync(Constants.AdminChatId, $"Неизвестная ошибка у {update.Message.From.Username}. Подробнее в логах");
                        }
                    }
                    
                    break;
                }
            }
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
            };
        }
    }
}