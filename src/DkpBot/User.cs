using System;
using System.Collections.Generic;
using System.Text;
using Amazon.DynamoDBv2.Model;

namespace DkpBot
{
    public class User
    {
        public long Id;
        public string TgLogin;
        public List<string> Characters;
        public Role Role;
        public int Adena;
        public int Dkp;
        public bool Active;
        public long ChatId;
        public DateTime CreationDateTime;

        public PutItemRequest ToPutItem(string tableName, bool overwrite = false)
        {
            var conditionExpression = !overwrite ? "attribute_not_exists(Id)" : "";
            var item =  new PutItemRequest
            {
                TableName = tableName,
                Item = new Dictionary<string,AttributeValue>() { 
                    { "Id", new AttributeValue {S = Id.ToString() }},
                    { "TgLogin", new AttributeValue {S = TgLogin}},
                    { "Role", new AttributeValue {N = ((int)Role).ToString()}},
                    { "Adena", new AttributeValue {N = (Adena).ToString()}},
                    { "Dkp", new AttributeValue {N = (Dkp).ToString()}},
                    { "Active", new AttributeValue {BOOL = Active}},
                    { "ChatId", new AttributeValue {N = ChatId.ToString()}},
                    { "CreationDateTime", new AttributeValue {S = CreationDateTime.ToString("yyyy-MM-ddTHH:mm:ss") }},
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
                Active = dict["Active"].BOOL,
                Adena = int.Parse(dict["Adena"].N),
                Dkp = int.Parse(dict["Dkp"].N),
                Id = long.Parse(dict["Id"].S),
                ChatId = int.Parse(dict["ChatId"].N),
                Role = (Role) int.Parse(dict["Role"].N),
                TgLogin = dict["TgLogin"].S,
                CreationDateTime = DateTime.Parse(dict["CreationDateTime"].S),
                Characters = dict["Characters"].SS,
            };
        }

        public string GetInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"TgLogin: {TgLogin}");
            sb.AppendLine($"Накоплено DKP: {Dkp}");
            sb.AppendLine($"Накоплено адены: {Adena}");
            sb.AppendLine($"Ваши персонажи: {string.Join(",", Characters)}");
            sb.AppendLine($"Ваш статус: {Role}");

            return sb.ToString();
        }
    }
}