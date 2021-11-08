using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        public static async Task<string> TryAddPartyLeader(User user, string partyLeaderId)
        {
            var partyLeaderName = await GetUserProperty(partyLeaderId, "Name");
            if (partyLeaderName == null)
                return null;    
            var partyLeadersPl = await GetUserProperty(partyLeaderId, "PL");
            if (partyLeadersPl != null && partyLeaderId != partyLeadersPl.S && user.Id.ToString() != partyLeaderId)
                return "cycleError";
            
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },
                ExpressionAttributeNames = new Dictionary<string,string>()
                {
                    {"#P", "PL"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":pl", new AttributeValue {S = partyLeaderId}},
                },
                UpdateExpression = "SET #P = :pl",
            };
                
            await DataBaseClient.UpdateItemAsync(req);

            return partyLeaderName.S;
        }
        
        public static async Task<MoveAdenaCommand.MoveEventResult> TryMoveAdena(User user, int adena)
        {
            if (string.IsNullOrEmpty(user.PartyLeader))
                return MoveAdenaCommand.MoveEventResult.NoPL;

            if (user.Adena < adena)
                return MoveAdenaCommand.MoveEventResult.NotEnough;
            
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":adena", new AttributeValue {N = (-adena).ToString()}},
                },
                UpdateExpression = "ADD Adena :adena",
            };
                
            await DataBaseClient.UpdateItemAsync(req);

            req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.PartyLeader}},
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":adena", new AttributeValue {N = adena.ToString()}},
                },
                UpdateExpression = "ADD Adena :adena",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
            
            return MoveAdenaCommand.MoveEventResult.Success;
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

        public static async Task<RenameResult> TryRenameHero(string heroName, int ownerId, string newName, string ownerName)
        {
            var heroItems = await GetItem(heroName, HeroTableName, "Id");

            if (heroItems.Count == 0)
            {
                return RenameResult.NoHero;
            }

            if (int.Parse(heroItems["ownerId"].N) != ownerId && ownerId != 107050210)
                return RenameResult.NoAccess;
            
            var heroItemsNew = await GetItem(newName.ToLower(), HeroTableName, "Id");
            if (heroItemsNew.Count != 0)
            {
                return RenameResult.NoAccessToNewName;
            }

            await TryAddHero(newName.ToLower(), ownerId, ownerName, newName);
            newName = newName.ToLower();
            await ChangeHeroCoefficient(newName.ToLower(), double.Parse(heroItems["coefficient"].N));
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

            foreach (var id in scanResponse.Items.Select(x => x["Id"].S))
            {
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
                
                updateReq = new UpdateItemRequest()
                {
                    TableName = UsersTableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        {"Id", new AttributeValue {S = id}},
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        {":heroName", new AttributeValue {SS = {newName}}}
                    },
                    UpdateExpression = "ADD Characters :heroName",
                };
                
                await DataBaseClient.UpdateItemAsync(updateReq);
            }

            return RenameResult.Success;
        }
        
         public static async Task<DeleteResult> TryDeleteHero(string heroName, int ownerId)
        {
            var heroItems = await GetItem(heroName, HeroTableName, "Id");

            if (heroItems.Count == 0)
            {
                return DeleteResult.NoHero;
            }

            if (int.Parse(heroItems["ownerId"].N) != ownerId && ownerId != 107050210)
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
        
        public static async Task SetMailAsync(string id, string mail)
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
                    {"#M", "Mail"}
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":mail", new AttributeValue {S = mail}}
                },
                UpdateExpression = "SET #M = :mail",
                ConditionExpression = "attribute_exists(Id)"
            };

            await DataBaseClient.UpdateItemAsync(req);
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

        private static async Task<AttributeValue> GetUserProperty(string id, string field)
        {
            return await GetProperty(id, UsersTableName, "Id", field);
        }
        
        private static async Task<AttributeValue> GetProperty(string id, string tableName, string primaryKey, string field)
        {
            var userResult =  await DataBaseClient.GetItemAsync(tableName,
                new Dictionary<string, AttributeValue>() {{primaryKey, new AttributeValue() {S = id}}});

            return !userResult.Item.ContainsKey(field) ? null : userResult.Item[field];
        }
        
        private static async Task<Dictionary<string, AttributeValue>> GetItem(string id, string tableName, string primaryKey)
        {
            var userResult =  await DataBaseClient.GetItemAsync(tableName,
                new Dictionary<string, AttributeValue>() {{primaryKey, new AttributeValue() {S = id}}});

            return userResult.Item;
        }

        public static async Task<string> TryCreateEventAsync(string eventName, string maxPeopleCount, int points, int raidLeaderId, int pvpPoints)
        {
            var sw =new Stopwatch();
            sw.Start();
            Dictionary<string, AttributeValue> item;
            string code;
            var i = 0;
            do
            {
                i++;
                code = CreateEventCommand.GetCode(4);
                item = await GetItem(code, EventsTableName, "Id");
                if (i > 20)
                {
                    LambdaLogger.Log("INFO: " + $"CreatingI {i}");
                }
                
            } while (item.Count != 0);
            LambdaLogger.Log("INFO: " + $"CreatingIdElapsed {sw.Elapsed.Milliseconds} {i} times");
            
            var ttl  = (long)(DateTime.UtcNow + TimeSpan.FromDays(28) - DateTime.UnixEpoch).TotalSeconds;
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
                    { "pvpPoints", new AttributeValue {N = pvpPoints.ToString()}},
                    { "ttl", new AttributeValue() {N = ttl.ToString()}}
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

        public static async Task<(JoinEventCommand.JoinEventResult, int, string, int)> TryJoinEventAsync(string code, string heroName, int fromId)
        {
           var eventItem =  await GetItem(code, EventsTableName, "Id");

           if (eventItem.Count == 0)
           {
               return (JoinEventCommand.JoinEventResult.NoEvent, 0, null, 0);
           }

           var creationTime = long.Parse(eventItem["сreationDateTimeSeconds"].N);
           var currentTime = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
           var eventContinueTime = 30;
           var registrationClosedAgo = currentTime - creationTime - TimeSpan.FromMinutes(eventContinueTime).TotalSeconds;
           if (registrationClosedAgo > 0)
           {
               return (JoinEventCommand.JoinEventResult.RegistrationClosed, (int)(registrationClosedAgo/60), null, 0);
           }

           var peopleCount = int.Parse(eventItem["maxPeopleCount"].N);
           int registered = 0;
           if (eventItem.ContainsKey("participants"))
           {
               registered = eventItem["participants"].SS.Count;
               if (eventItem["participants"].SS.Contains(heroName))
                   return (JoinEventCommand.JoinEventResult.HeroDuplicate, 0, null, 0);
           }

           var fromIdStr = fromId.ToString();
           if (eventItem.ContainsKey("userIds"))
           {
               registered = eventItem["userIds"].SS.Count;
               if (eventItem["userIds"].SS.Contains(fromIdStr))
                   return (JoinEventCommand.JoinEventResult.UserDuplicate, 0, null, 0);
           }
           
           if (peopleCount == registered)
           {
               return (JoinEventCommand.JoinEventResult.TooManyPeople, registered, null, 0);
           }
           
           var userItem = await GetItem(fromIdStr, UsersTableName, "Id");
           var heroes = userItem["Characters"]?.SS ?? new List<string>();
           if (!heroes.Contains(heroName))
               return (JoinEventCommand.JoinEventResult.NoAccessToHero, 0, null, 0);

           var points = eventItem["dkpPoints"].N;
           var pvpPoints = eventItem["pvpPoints"].N;
           var eventName = eventItem["eventName"].S;
           
           var heroItem = await GetItem(heroName, HeroTableName, "Id");
           var coefficient = double.Parse(heroItem["coefficient"].N);
           var peopleCoeff = 1.0;
           var maxPeopleCount = int.Parse(eventItem["maxPeopleCount"].N);
           if (maxPeopleCount < 15)
               peopleCoeff = 1.2;
           if (maxPeopleCount < 10)
               peopleCoeff = 1.5;
           
           points = Math.Floor(int.Parse(points) * coefficient * peopleCoeff).ToString(CultureInfo.InvariantCulture);
           
           UpdateItemRequest req = new UpdateItemRequest
           {
               TableName = UsersTableName,
               Key = new Dictionary<string, AttributeValue>()
               {
                   {"Id", new AttributeValue {S = fromIdStr}},
               },
               ExpressionAttributeNames = new Dictionary<string,string>()
               {
                   {"#D", "Dkp"},
                   {"#P", "PvpPoints"},
                   {"#TP", "TotalPvpPoints"},
                   {"#E", "EventsVisited"},
                   {"#TE", "TotalEventsVisited"},
               },
               ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
               {
                   {":dkp", new AttributeValue {N = points}},
                   {":pvpPoints", new AttributeValue {N = pvpPoints}},
                   {":1", new AttributeValue {N = "1"}},
               },
               UpdateExpression = "ADD #D :dkp, #P :pvpPoints, #TP :pvpPoints, #E :1, #TE :1",
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
                   {"#PP", "participantPoints"},
                   {"#U", "userIds"},
               },
               ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
               {
                   {":h", new AttributeValue {SS = {heroName}}},
                   {":pp", new AttributeValue {SS = {$"{fromIdStr}::{points}"}}},
                   {":u", new AttributeValue {SS = {fromIdStr}}},
               },
               UpdateExpression = "ADD #P :h, #U :u, #PP :pp",
               //UpdateExpression = "ADD #P :h",
           };
                
           await DataBaseClient.UpdateItemAsync(req2);
           return (JoinEventCommand.JoinEventResult.Success, int.Parse(points), eventName, int.Parse(pvpPoints));
        }

        public static async Task<User> ChangeHeroCoefficient(string heroName, double coeff)
        {
            var heroItem =  await GetItem(heroName, HeroTableName, "Id");
            if (heroItem.Count == 0)
                return null;
            
            UpdateItemRequest req2 = new UpdateItemRequest
            {
                TableName = HeroTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = heroName}},
                },

                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":c", new AttributeValue {N = coeff.ToString(CultureInfo.InvariantCulture)}}
                },
                UpdateExpression = "SET coefficient = :c",
            };
                
            await DataBaseClient.UpdateItemAsync(req2);
            
            var userResultAsync = await DBHelper.GetUserResultAsync(heroItem["ownerId"].N);
            var user = User.FromDict(userResultAsync.Item);
            return user;
        }

        public static async Task<List<User>> GetTopAsync(int count, bool isAdenaSort)
        {
            var scanResponse = await DataBaseClient.ScanAsync(UsersTableName, new Dictionary<string, Condition>());
            
            return scanResponse.Items
                .Select(User.FromDict)
                .OrderByDescending(x => isAdenaSort ? x.Adena : x.Dkp)
                .ThenByDescending(x => isAdenaSort ? x.Dkp : x.Adena)
                .Take(count)
                .ToList();
        }

        public static async Task<(User[] users, double coef, int totalDkp)> ConvertDkpToAdena(int adena)
        {
            var scanResponse = await DataBaseClient.ScanAsync(UsersTableName, new Dictionary<string, Condition>());
            var users = scanResponse.Items
                .Select(User.FromDict)
                .ToArray();
            var totalDkp = users.Sum(x => x.Dkp);
            var coef = (double)adena / totalDkp;
            return (users, coef, totalDkp);
        }

        public static async Task<User[]> GetAllUsers()
        {
            var scanResponse = await DataBaseClient.ScanAsync(UsersTableName, new Dictionary<string, Condition>());
            var users = scanResponse.Items
                .Select(User.FromDict)
                .ToArray();
            return users;
        }
        
        public static async Task CleanAllCoefsAsync()
        {
            var scanResponse = await DataBaseClient.ScanAsync(HeroTableName, new Dictionary<string, Condition>());
            var heroes = scanResponse.Items
                .Select(x => x["Id"].S)
                .ToArray();
            foreach (var hero in heroes)
            {
                UpdateItemRequest req = new UpdateItemRequest
                {
                    TableName = HeroTableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        {"Id", new AttributeValue {S = hero}},
                    },
                
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        {":z", new AttributeValue {N = 1.ToString()}}
                    },

                    UpdateExpression = "SET coefficient = :z",
                };
                
                await DataBaseClient.UpdateItemAsync(req);
            }
        }
        

        public static async Task<int> ConvertDkpToAdenaForUser(User user, double coef)
        {
            var resultAdena = (int)Math.Floor(user.Dkp*coef);
            UpdateItemRequest req = new UpdateItemRequest
            {
                
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },

                
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":dkp", new AttributeValue {N = (-user.Dkp).ToString()}},
                    {":adena", new AttributeValue {N = (resultAdena).ToString()}},
                },
                UpdateExpression = "ADD Dkp :dkp, Adena :adena",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
            return resultAdena;
        }
        
        public static async Task CleanStatsAsync(User user)
        {
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },
                
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":z",new AttributeValue {N = 0.ToString()}}
                },

                UpdateExpression = "SET EventsVisited = :z, PvpPoints = :z",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
        }
        
        public static async Task CleanAdenaAndDkpAsync(User user)
        {
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },
                
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":z",new AttributeValue {N = 0.ToString()}}
                },

                UpdateExpression = "SET Adena = :z, Dkp = :z",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
        }
        
        public static async Task CleanTotalStatsAsync(User user)
        {
            UpdateItemRequest req = new UpdateItemRequest
            {
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = user.Id.ToString()}},
                },
                
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":z",new AttributeValue {N = 0.ToString()}}
                },

                UpdateExpression = "SET EventsVisited = :z, TotalEventsVisited = :z, PvpPoints = :z, TotalPvpPoints = :z",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
        }

        public static async Task<int> ChangeEventPeopleCount(string code, int peopleCount)
        {
            var eventItem =  await GetItem(code, EventsTableName, "Id");
            if (eventItem.Count == 0)
                return -1;
            
            var oldPeopleCount = int.Parse(eventItem["maxPeopleCount"].N);
            if (oldPeopleCount == peopleCount)
                return oldPeopleCount;
            
            UpdateItemRequest req = new UpdateItemRequest
            {
                
                TableName = EventsTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = code}},
                },

                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":p", new AttributeValue {N = peopleCount.ToString()}},
                },
                UpdateExpression = "SET maxPeopleCount = :p",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
            return oldPeopleCount;
        }

        public static async Task AddAdenaAsync(string id, int adena)
        {
            UpdateItemRequest req = new UpdateItemRequest
            {
                
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = id}},
                },

                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":a", new AttributeValue {N = adena.ToString()}},
                },
                UpdateExpression = "ADD Adena :a",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
        }

        public static async Task<Event[]> GetEventsAsync(int fromDaysAgo)
        {
            var currentTime = (DateTime.UtcNow - TimeSpan.FromDays(fromDaysAgo) - DateTime.UnixEpoch).TotalSeconds;

            var scanFilter = new Dictionary<string, Condition>()
            {
                {"сreationDateTimeSeconds", new Condition()
                {
                    ComparisonOperator = "GE", AttributeValueList = new List<AttributeValue>()
                    {
                        new AttributeValue {N = currentTime.ToString(CultureInfo.InvariantCulture)}
                    }
                }}
            };

            var scanResponse = await DataBaseClient.ScanAsync(EventsTableName, scanFilter);

            var events = scanResponse.Items
                .Select(Event.FromDict)
                .ToArray();
            
            return events;
        }

        public static async Task DeleteEventAsync(string code)
        {
            var request = new DeleteItemRequest()
            {
                TableName = EventsTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {
                        "Id", new AttributeValue()
                        {
                            S = code
                        }
                    }
                }
            };
            
            await DataBaseClient.DeleteItemAsync(request);
        }
        
        public static async Task DeleteEventsAsync(int untilDays)
        {
            var currentTime = (DateTime.UtcNow - TimeSpan.FromDays(untilDays) - DateTime.UnixEpoch).TotalSeconds;

            var scanFilter = new Dictionary<string, Condition>()
            {
                {"сreationDateTimeSeconds", new Condition()
                {
                    ComparisonOperator = "LE", AttributeValueList = new List<AttributeValue>()
                    {
                        new AttributeValue {N = currentTime.ToString(CultureInfo.InvariantCulture)}
                    }
                }}
            };

            var scanResponse = await DataBaseClient.ScanAsync(EventsTableName, scanFilter);

            LambdaLogger.Log("INFO: " + $"Deleting first 50 from {scanResponse.Items.Count()} items from table {EventsTableName}");
            int i = 0;
            foreach (var item in scanResponse.Items)
            {
                i++;
                if (i > 50)
                    break;
                var request = new DeleteItemRequest()
                {
                    TableName = EventsTableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        {
                            "Id", new AttributeValue()
                            {
                                S = item["Id"].S
                            }
                        }
                    }
                };
                LambdaLogger.Log("INFO: " + $"Deleting { item["Id"].S}");
                await DataBaseClient.DeleteItemAsync(request);
                Thread.Sleep(500);
            }
            LambdaLogger.Log("INFO: " + $"Deleteing finsihed");
        }

        public static async Task<Dictionary<string, AttributeValue>> GetUserResultByNameAsync(string name)
        {
            var scanFilter = new Dictionary<string, Condition>()
            {
                {"Name", new Condition()
                {
                    ComparisonOperator = "EQ", AttributeValueList = new List<AttributeValue>()
                    {
                        new AttributeValue {S = name}
                    }
                }}
            };

            var scanResponse = await DataBaseClient.ScanAsync(UsersTableName, scanFilter);

            return scanResponse.Items.FirstOrDefault();
        }

        public static async Task AddDkpAsync(string id, int dkp)
        {
           
            UpdateItemRequest req = new UpdateItemRequest
            {
                
                TableName = UsersTableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {"Id", new AttributeValue {S = id}},
                },

                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    {":d", new AttributeValue {N = dkp.ToString()}},
                },
                UpdateExpression = "ADD Dkp :d",
            };
                
            await DataBaseClient.UpdateItemAsync(req);
        }

        public static async Task<List<string>> GetEventUsersAsync(string code)
        {
            var eventItem =  await GetItem(code, EventsTableName, "Id");

            if (eventItem.Count == 0)
            {
                return null;
            }

            return eventItem["participants"].SS;
        }
        
        public static async Task<List<string>> GetEventUsersWithPointsAsync(string code)
        {
            var eventItem =  await GetItem(code, EventsTableName, "Id");

            if (eventItem.Count == 0)
            {
                return null;
            }

            return eventItem["participantPoints"].SS;
        }

        public static async Task<Event[]> GetAllEveningPvpEventsAsync(int fromDaysAgo)
        {
            var currentTime = (DateTime.UtcNow - TimeSpan.FromDays(fromDaysAgo) - DateTime.UnixEpoch).TotalSeconds;

            var scanFilter = new Dictionary<string, Condition>()
            {
                {"сreationDateTimeSeconds", new Condition()
                {
                    ComparisonOperator = "GE", AttributeValueList = new List<AttributeValue>()
                    {
                        new AttributeValue {N = currentTime.ToString(CultureInfo.InvariantCulture)}
                    }
                }}
            };

            var scanResponse = await DataBaseClient.ScanAsync(EventsTableName, scanFilter);

            
            
            var events = scanResponse.Items
                .Select(Event.FromDict)
                .Where(x=>x.dkpPoints > 0)
                .ToArray();
            
            return events;
        }
    }
}