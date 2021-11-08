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
        public int Adena;
        public int Dkp;
        public int PvpPoints;
        public int TotalPvpPoints;
        public int EventsVisited;
        public int TotalEventsVisited;
        public bool Active;
        public long ChatId;
        public DateTime CreationDateTime;
        public string PartyLeader;
        public string Mail;

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
                    { "Role", new AttributeValue {N = ((int)Role).ToString()}},
                    { "Adena", new AttributeValue {N = (Adena).ToString()}},
                    { "Dkp", new AttributeValue {N = (Dkp).ToString()}},
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
                Adena = int.Parse(dict["Adena"].N),
                Dkp = int.Parse(dict["Dkp"].N),
                Id = long.Parse(dict["Id"].S),
                ChatId = int.Parse(dict["ChatId"].N),
                Role = (Role) int.Parse(dict["Role"].N),
                TgLogin = dict["TgLogin"].S,
                Name = dict["Name"].S,
                CreationDateTime = DateTime.Parse(dict["CreationDateTime"].S),
                Characters = dict.ContainsKey("Characters") ? dict["Characters"].SS : new List<string>(),
                PartyLeader = dict.ContainsKey("PL") ? dict["PL"].S : "",
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
            sb.AppendLine($"Накоплено адены: {Adena}кк");
            sb.AppendLine($"Накоплено pvp-очков: {PvpPoints}");
            sb.AppendLine($"Рейтинг пвп-активности: {100.0*(float)PvpPoints/EventsVisited, 2}");
            sb.AppendLine(!string.IsNullOrEmpty(PartyLeader) ? $"Ваш ПЛ: {PartyLeader}" : $"Ваш ПЛ: отсутствует");
            sb.AppendLine($"Ваши персонажи: {string.Join(",", Characters)}");
            sb.AppendLine($"Ваш статус: {Role}");
            sb.AppendLine($"Персонаж для получения почты: {Mail}");
            sb.AppendLine($"Всего посещено событий: {TotalEventsVisited}");
            sb.AppendLine($"Pvp-очков за все время: {TotalPvpPoints}");
            sb.AppendLine($"Рейтинг пвп-активности за все время: {100.0*(float)TotalPvpPoints/TotalEventsVisited, 2}");

            return sb.ToString();
        }
    }
}