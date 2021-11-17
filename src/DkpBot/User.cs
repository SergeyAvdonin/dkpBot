using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

namespace DkpBot
{
    public class User
    {
        public long Id;
        public string TgLogin;
        public string Name;
        public List<string> Characters;
        public Role Role;
        public int Dkp;
        public int WorldDkp;
        public int PvpPoints;
        public int TotalPvpPoints;
        public int EventsVisited;
        public int TotalEventsVisited;
        public bool Active;
        public long ChatId;
        public DateTime CreationDateTime;
        public string PartyLeader;
        public string Mail;
        public string ConstParty;

        public PutItemRequest ToPutItem(string tableName, bool overwrite = false)
        {
            var conditionExpression = !overwrite ? "attribute_not_exists(Id)" : "";
            LambdaLogger.Log(JsonConvert.SerializeObject(this));
            if (string.IsNullOrEmpty(TgLogin))
                TgLogin = "@No_Name"; 
            var item =  new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string,AttributeValue>() { 
                    { "Id", new AttributeValue {S = Id.ToString() }},
                    { "TgLogin", new AttributeValue {S = TgLogin}},
                    { "Name", new AttributeValue {S = Name}},
                    { "ConstParty", new AttributeValue {S = ConstParty ?? ""}},
                    { "Role", new AttributeValue {N = ((int)Role).ToString()}},
                    { "Dkp", new AttributeValue {N = (Dkp).ToString()}},
                    { "WorldDkp", new AttributeValue {N = (WorldDkp).ToString()}},
                    { "Active", new AttributeValue {BOOL = Active}},
                    { "ChatId", new AttributeValue {N = ChatId.ToString()}},
                    { "CreationDateTime", new AttributeValue {S = CreationDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") }},
                    { "PvpPoints", new AttributeValue { N = (PvpPoints).ToString() }},
                    { "TotalPvpPoints", new AttributeValue { N = (TotalPvpPoints).ToString() }},
                    { "EventsVisited", new AttributeValue { N = (EventsVisited).ToString() }},
                    { "TotalEventsVisited", new AttributeValue { N = (TotalEventsVisited).ToString() }},
                },

                // Every item must have the key attributes, so using 'attribute_not_exists'
                // on a key attribute is functionally equivalent to an "item_not_exists" 
                // condition, causing the PUT to fail if it would overwrite anything at all.
                ConditionExpression = conditionExpression
            };
            if (Characters.Count != 0)
            {
                item.Item.Add("Characters", new AttributeValue {SS = Characters});
            }

            return item;
        }

        public static User FromDict(Dictionary<string, AttributeValue> dict)
        {
            return new User()
            {
                Active = dict.ContainsKey("Active") && dict["Active"].BOOL,
                WorldDkp = dict.ContainsKey("WorldDkp") ? int.Parse(dict["WorldDkp"].N) : 0,
                Dkp = int.Parse(dict["Dkp"].N),
                Id = long.Parse(dict["Id"].S),
                ChatId = int.Parse(dict["ChatId"].N),
                Role = (Role) int.Parse(dict["Role"].N),
                TgLogin = dict["TgLogin"].S,
                Name = dict["Name"].S,
                ConstParty = dict.ContainsKey("ConstParty") ? dict["ConstParty"].S : "",
                CreationDateTime = DateTime.Parse(dict["CreationDateTime"].S),
                Characters = dict.ContainsKey("Characters") ? dict["Characters"].SS : new List<string>(),
                Mail = dict.ContainsKey("Mail") ? dict["Mail"].S : "",
                PvpPoints = dict.ContainsKey("PvpPoints") ? int.Parse(dict["PvpPoints"].N) : 0,
                TotalPvpPoints = dict.ContainsKey("TotalPvpPoints") ? int.Parse(dict["TotalPvpPoints"].N) : 0,
                EventsVisited = dict.ContainsKey("EventsVisited") ? int.Parse(dict["EventsVisited"].N) : 0,
                TotalEventsVisited = dict.ContainsKey("TotalEventsVisited") ? int.Parse(dict["TotalEventsVisited"].N) : 0,
            };
        }

        public string GetInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Имя: {Name}");
            sb.AppendLine($"Telegram: {TgLogin}");
            sb.AppendLine($"Накоплено DKP: {Dkp}");
            sb.AppendLine($"Накоплено DKP межсервер: {WorldDkp}");

            sb.AppendLine(!string.IsNullOrEmpty(ConstParty) ? $"Ваша кп: {ConstParty}" : $"Ваша кп: отсутствует");
            sb.AppendLine($"Ваши персонажи: {string.Join(",", Characters)}");
            sb.AppendLine($"Ваш статус: {Role}");
            sb.AppendLine($"Персонаж для получения почты: {Mail}");
            sb.AppendLine($"Всего посещено событий: {TotalEventsVisited}");

            return sb.ToString();
        }
    }
}