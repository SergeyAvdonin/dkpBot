using System;
using System.Collections.Generic;
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

        public static async Task<string> TryAddHero(string heroName, int ownerId, string ownerName)
        {
            var heroResult = await DataBaseClient.GetItemAsync(HeroTableName,
                new Dictionary<string, AttributeValue>() {{"heroName", new AttributeValue() {S = heroName}}});
            
            if (heroResult.Item.Count == 0)
            {
                var putItemRequest = new PutItemRequest
                {
                    TableName = HeroTableName,
                    Item = new Dictionary<string,AttributeValue>() {
                        { "heroName", new AttributeValue {S = heroName }},
                        { "coefficient", new AttributeValue {N = 1.ToString()}},
                        { "ownerId", new AttributeValue {N = ownerId.ToString()}},
                        { "ownerName", new AttributeValue {S = ownerName}},
                    },

                    // Every item must have the key attributes, so using 'attribute_not_exists'
                    // on a key attribute is functionally equivalent to an "item_not_exists" 
                    // condition, causing the PUT to fail if it would overwrite anything at all.
                    ConditionExpression = "attribute_not_exists(heroName)"
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

        public static async Task ChangeRoleAsync(string id, string role)
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
        }

        public static async Task<(ShareResult, string)> TryShareHeroAsync(string heroName, string idToShare, string fromId, string fromUsername)
        {
            var ownerName = "";
            var heroResult = await DataBaseClient.GetItemAsync(HeroTableName,
                new Dictionary<string, AttributeValue>() {{"heroName", new AttributeValue() {S = heroName}}});

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

            if (userResult.Item["Characters"].SS.Contains(heroName))
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
            var userResult =  await DataBaseClient.GetItemAsync(UsersTableName,
                new Dictionary<string, AttributeValue>() {{"Id", new AttributeValue() {S = id}}});

            return userResult.Item[field];
        }
    }
}