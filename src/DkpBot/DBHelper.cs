using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using DkpBot.Commands;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace DkpBot
{
    public static class DBHelper
    {
        private static AmazonDynamoDBClient DataBaseClient;
        private static string UsersTableName = "Users";
        private static string HeroTableName = "Heroes";
        private static string EventsTableName = "Events";
        public static void Init()
        {
            AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                Timeout = TimeSpan.FromSeconds(20),
                MaxErrorRetry = 3
            };
            DataBaseClient = new AmazonDynamoDBClient(clientConfig);
        }

        public static async Task<bool> RegisterUser(User user)
        {
            var putItemRequest = user.ToPutItem(UsersTableName);

            try
            {
                var putItemResponse = await DataBaseClient.PutItemAsync(putItemRequest);
                return true;
            }
            catch (ConditionalCheckFailedException ex)
            {
                return false;
            }
        }

        public static async Task<string> TryAddHero(string heroName, int ownerId, string ownerName, string prettyName)
        {
            var scanReq = new ScanRequest()
            {
                TableName = UsersTableName,
                ExpressionAttributeNames = new Dictionary<string,string>()
                {
                    {"#Characters", "Characters"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":heroName", new AttributeValue {S = heroName}}
                },
                FilterExpression = "contains(#Characters, :heroName)"
            };

            var scanResponse = await DataBaseClient.ScanAsync(scanReq);
            var heroResult = await DataBaseClient.GetItemAsync(HeroTableName,
                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = heroName}}});
            
            if (heroResult.Item.Count == 0)
            {
                var putItemRequest = new PutItemRequest
                {
                    TableName = HeroTableName,
                    Item = new Dictionary<string,AttributeValue>() {
                        { "Id", new AttributeValue {S = heroName }},
                        { "coefficient", new AttributeValue {N = 1.ToString()}},
                        { "ownerId", new AttributeValue {N = ownerId.ToString()}},
                        { "ownerName", new AttributeValue {S = ownerName}},
                        { "prettyName", new AttributeValue {S = prettyName}},
                    },

                    // Every item must have the key attributes, so using 'attribute_not_exists'
                    // on a key attribute is functionally equivalent to an "item_not_exists" 
                    // condition, causing the PUT to fail if it would overwrite anything at all.
                    ConditionExpression = "attribute_not_exists(Id)"
                };
                
                LambdaLogger.Log("INFO: " + $"Putting {heroName} to table {HeroTableName}");
                var putItemResponse = await DataBaseClient.PutItemAsync(putItemRequest);
                
                var userResult =  await DataBaseClient.GetItemAsync(UsersTableName,
                    new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = ownerId.ToString()}}});

                if (userResult.Item.Count == 0)
                {
                    throw new Exception("can't find user for hero");
                }

                UpdateItemRequest req = new UpdateItemRequest
                {
                    TableName = UsersTableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        {"Id", new AttributeValue {S = ownerId.ToString()}},
                    },
                    ExpressionAttributeNames = new Dictionary<string,string>()
                    {
                        {"#C", "Characters"}
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        {":character",new AttributeValue {SS = {heroName}}},
                    },
                    UpdateExpression = "ADD #C :character",
                };
                
                await DataBaseClient.UpdateItemAsync(req);
                
                return null;
            }
            else
            {
                var item = heroResult.Item["ownerName"];
                var owner = item.S;
                return owner;
            }
            
        }

        public static async Task<DeleteResult> TryDeleteHero(string heroName, int ownerId)
        {
            var heroItems = await GetItem(heroName, HeroTableName, "Id");

            if (heroItems.Count == 0)
            {
                return DeleteResult.NoHero;
            }

            if (int.Parse(heroItems["ownerId"].N) != ownerId)
                return DeleteResult.NoAccess;

            var request = new DeleteItemRequest
            {
                TableName = HeroTableName,
                Key = new Dictionary<string,AttributeValue>() { { "Id", new AttributeValue { S = heroName } } },
            };

            var response = await DataBaseClient.DeleteItemAsync(request);
            
            var scanReq = new ScanRequest()
            {
                TableName = UsersTableName,
                ExpressionAttributeNames = new Dictionary<string,string>()
                {
                    {"#Characters", "Characters"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":heroName", new AttributeValue {S = heroName}}
                },
                FilterExpression = "contains(#Characters, :heroName)"
            };

            var scanResponse = await DataBaseClient.ScanAsync(scanReq);
            LambdaLogger.Log($"было найдено {scanResponse.Items.Count} чуваков");
            LambdaLogger.Log(JsonConvert.SerializeObject(scanResponse));
            foreach (var id in scanResponse.Items.Select(x => x["Id"].S))
            {
                LambdaLogger.Log($"чувак с id = {id}");
                var updateReq = new UpdateItemRequest()
                {
                    TableName = UsersTableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        {"Id", new AttributeValue {S = id}},
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        {":heroName", new AttributeValue {SS = {heroName}}}
                    },
                    UpdateExpression = "DELETE Characters :heroName",
                };
                
                await DataBaseClient.UpdateItemAsync(updateReq);
            }

            return DeleteResult.Success;
        }

                
        public static async Task<GetItemResponse> GetUserResultAsync(string id)
        {
            return await DataBaseClient.GetItemAsync(UsersTableName,
                                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = id}}});
        }
        public static async Task<Role> GetRole(int userId)
        {
            var userResult =  await DataBaseClient.GetItemAsync(UsersTableName,
                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = userId.ToString()}}});
            if (userResult.Item.Count == 0)
            {
                return Role.NotRegistered;
            }
            else
            {
                return (Role)int.Parse(userResult.Item["Role"].N);
            }
        }

        public static async Task<string> ChangeRoleAsync(string id, string role)
        {
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = id}},
                },
                ExpressionAttributeNames = new Dictionary<string,string>()
                {
                    {"#R", "Role"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":role",new AttributeValue {N = role}}
                },
                UpdateExpression = "SET #R = :role",
                ConditionExpression = "attribute_exists(Id)"
            };

            await DataBaseClient.UpdateItemAsync(req);

            var item = await GetItem(id, UsersTableName, "Id");
            return item["ChatId"].N;
        }

        public static async Task<(ShareResult, string)> TryShareHeroAsync(string heroName, string idToShare, string fromId)
        {
            var ownerName = "";
            var heroResult = await DataBaseClient.GetItemAsync(HeroTableName,
                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = heroName}}});

            if (heroResult.Item.Count == 0)
            {
                ownerName = "";
                return (ShareResult.NoAccess, ownerName);
            }

            if (heroResult.Item["ownerId"].N != fromId)
            {
                ownerName = heroResult.Item["ownerName"].S;
                return(ShareResult.NoAccess, ownerName);
            }
            
            
            var userResult =  await DataBaseClient.GetItemAsync(UsersTableName,
                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = idToShare}}});
            
            if (userResult.Item.Count == 0)
            {
                return (ShareResult.NoUser, ownerName);
            }

            if (userResult.Item.ContainsKey("Characters") && userResult.Item["Characters"].SS.Contains(heroName))
            {
                return (ShareResult.AlreadyHas, ownerName);
            }
            
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = idToShare}},
                },
                ExpressionAttributeNames = new Dictionary<string,string>()
                {
                    {"#C", "Characters"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":character", new AttributeValue {SS = {heroName}}},
                },
                UpdateExpression = "ADD #C :character",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
            
            return (ShareResult.Success, ownerName);
        }

        public static async Task<long> GetChatIdAsync(string id)
        {
            var userProperty = await GetUserProperty(id, "ChatId");
            return long.Parse(userProperty.N);
        }

        public static async Task<AttributeValue> GetUserProperty(string id, string field)
        {
            return await GetProperty(id, UsersTableName, "Id", field);
        }
        
        private static async Task<AttributeValue> GetProperty(string id, string tableName, string primaryKey, string field)
        {
            var userResult =  await DataBaseClient.GetItemAsync(tableName,
                new Dictionary<string, AttributeValue>() {{primaryKey, new AttributeValue() {S = id}}});

            return userResult.Item[field];
        }
        
        private static async Task<Dictionary<string, AttributeValue>> GetItem(string id, string tableName, string primaryKey)
        {
            var userResult =  await DataBaseClient.GetItemAsync(tableName,
                new Dictionary<string, AttributeValue>() {{primaryKey, new AttributeValue() {S = id}}});

            return userResult.Item;
        }

        public static async Task<string> TryCreateEventAsync(string eventName, string maxPeopleCount, int points, int raidLeaderId)
        {
            var sw =new Stopwatch();
            sw.Start();
            Dictionary<string, AttributeValue> item;
            string code;
            var i = 0;
            do
            {
                i++;
                code = CreateEventCommand.GetCode(6);
                item = await GetItem(code, EventsTableName, "Id");
                if (i > 20)
                {
                    LambdaLogger.Log("INFO: " + $"CreatingI {i}");
                }
                
            } while (item.Count != 0);
            LambdaLogger.Log("INFO: " + $"CreatingIdElapsed {sw.Elapsed.Milliseconds} {i} times");
            
            var putItemRequest = new PutItemRequest
            {
                TableName = EventsTableName,
                Item = new Dictionary<string,AttributeValue>() {
                    { "Id", new AttributeValue {S = code }},
                    { "eventName", new AttributeValue {S = eventName}},
                    { "maxPeopleCount", new AttributeValue {N = maxPeopleCount}},
                    { "dkpPoints", new AttributeValue {N = points.ToString()}},
                    { "raidLeaderId", new AttributeValue {N = raidLeaderId.ToString()}},
                    { "сreationDateTime", new AttributeValue {S = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }},
                    { "сreationDateTimeSeconds", new AttributeValue {N = ((long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds).ToString(CultureInfo.InvariantCulture) }},
                },

                // Every item must have the key attributes, so using 'attribute_not_exists'
                // on a key attribute is functionally equivalent to an "item_not_exists" 
                // condition, causing the PUT to fail if it would overwrite anything at all.
                ConditionExpression = "attribute_not_exists(Id)"
            };
                
            LambdaLogger.Log("INFO: " + $"Creating {eventName} {sw.ElapsedMilliseconds}");
            var putItemResponse = await DataBaseClient.PutItemAsync(putItemRequest);
            LambdaLogger.Log("INFO: " + $"Creating {eventName} finsihed {sw.ElapsedMilliseconds}");
            return code;
        }

        public static async Task<(JoinEventCommand.JoinEventResult, int)> TryJoinEventAsync(string code, string heroName, int fromId)
        {
           var eventItem =  await GetItem(code, EventsTableName, "Id");

           if (eventItem.Count == 0)
           {
               return (JoinEventCommand.JoinEventResult.NoEvent, 0);
           }

           var creationTime = long.Parse(eventItem["сreationDateTimeSeconds"].N);
           var currentTime = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
           var eventContinueTime = 20;
           var registrationClosedAgo = currentTime - creationTime - TimeSpan.FromMinutes(eventContinueTime).TotalSeconds;
           if (registrationClosedAgo > 0)
           {
               return (JoinEventCommand.JoinEventResult.RegistrationClosed, (int)(registrationClosedAgo/60));
           }

           var peopleCount = int.Parse(eventItem["maxPeopleCount"].N);
           int registered = 0;
           if (eventItem.ContainsKey("participants"))
           {
               registered = eventItem["participants"].SS.Count;
               if (eventItem["participants"].SS.Contains(heroName))
                   return (JoinEventCommand.JoinEventResult.HeroDuplicate, 0);
           }

           var fromIdStr = fromId.ToString();
           if (eventItem.ContainsKey("userIds"))
           {
               registered = eventItem["userIds"].SS.Count;
               if (eventItem["userIds"].SS.Contains(fromIdStr))
                   return (JoinEventCommand.JoinEventResult.UserDuplicate, 0);
           }
           
           if (peopleCount == registered)
           {
               return (JoinEventCommand.JoinEventResult.TooManyPeople, registered);
           }
           
           var userItem = await GetItem(fromIdStr, UsersTableName, "Id");
           var heroes = userItem["Characters"]?.SS ?? new List<string>();
           if (!heroes.Contains(heroName))
               return (JoinEventCommand.JoinEventResult.NoAccessToHero, 0);

           var points = eventItem["dkpPoints"].N;
           
           UpdateItemRequest req = new UpdateItemRequest
           {
               TableName = UsersTableName,
               Key = new Dictionary<string, AttributeValue>()
               {
                   {"Id", new AttributeValue {S = fromIdStr}},
               },
               ExpressionAttributeNames = new Dictionary<string,string>()
               {
                   {"#D", "Dkp"}
               },
               ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
               {
                   {":dkp", new AttributeValue {N = points}},
               },
               UpdateExpression = "ADD #D :dkp",
           };
                
           await DataBaseClient.UpdateItemAsync(req);
           
           UpdateItemRequest req2 = new UpdateItemRequest
           {
               TableName = EventsTableName,
               Key = new Dictionary<string, AttributeValue>()
               {
                   {"Id", new AttributeValue {S = code}},
               },
               ExpressionAttributeNames = new Dictionary<string,string>()
               {
                   {"#P", "participants"},
                   {"#U", "userIds"},
               },
               ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
               {
                   {":h", new AttributeValue {SS = {heroName}}},
                   {":u", new AttributeValue {SS = {fromIdStr}}},
               },
               UpdateExpression = "ADD #P :h, #U :u",
               //UpdateExpression = "ADD #P :h",
           };
                
           await DataBaseClient.UpdateItemAsync(req2);
           return (JoinEventCommand.JoinEventResult.Success, int.Parse(points));
        }
    }
}