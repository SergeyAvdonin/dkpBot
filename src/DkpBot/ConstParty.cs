using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

namespace DkpBot
{
    public class ConstParty
    {
        public string Name;
        public List<User> Members;
        public string Pl;
        public int TotalPoints;
        public int TotalWorldPoints;

        public PutItemRequest ToPutItem(string tableName, bool overwrite = false)
        {
            throw new NotImplementedException();
        }

        public static async Task<ConstParty> FromDict(Dictionary<string, AttributeValue> dict)
        {
            var pl = dict["pl"].N;
            var memberNames = dict.ContainsKey("members") ? dict["members"].NS : new List<string>();

            var user =  User.FromDict((await DBHelper.GetUserResultAsync(pl)).Item);
            var members = memberNames.Select(x => User.FromDict((DBHelper.GetUserResultAsync(x).GetAwaiter().GetResult()).Item)).OrderBy(x=>x.Name == pl).ToList();
            return new ConstParty()
            {
                Name = dict["name"].S,
                Pl = user.Name,
                Members = members,
                TotalPoints = members.Sum(x=>x.Dkp),
                TotalWorldPoints = members.Sum(x=>x.WorldDkp),
            };
        }

        public string GetInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Имя: {Name}");
            sb.AppendLine($"Накоплено DKP: {TotalPoints}");
            sb.AppendLine($"Накоплено DKP межсервер: {TotalWorldPoints}");
            
            sb.AppendLine($"Участники кп:\n{string.Join("\n", Members.Select(x=>$"{x.Name} : {x.Dkp} дкп{GetPl(x)}"))}");

            return sb.ToString();
        }

        public string GetPl(User user)
        {
            return user.Name == Pl ? " (ПЛ)" : "";
        }
    }
}