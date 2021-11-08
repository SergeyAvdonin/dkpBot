using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace DkpBot
{
    public class Event
    {
        public string Id;
        public int dkpPoints;
        public int pvpPoints;
        public string eventName;
        public int maxPeopleCount;
        public int raidLeaderId;
        public long creationDateTimeSeconds;
        public string creationDateTime;
        public string[] participants;
        
        public static Event FromDict(Dictionary<string, AttributeValue> dict)
        {
            return new Event()
            {
                Id = dict["Id"].S,
                dkpPoints = int.Parse(dict["dkpPoints"].N),
                pvpPoints = int.Parse(dict["pvpPoints"].N),
                eventName = dict["eventName"].S,
                maxPeopleCount = int.Parse(dict["maxPeopleCount"].N),
                raidLeaderId = int.Parse(dict["raidLeaderId"].N),
                creationDateTimeSeconds = long.Parse(dict["сreationDateTimeSeconds"].N),
                creationDateTime = dict["сreationDateTime"].S,
                participants = dict.ContainsKey("participants") ? dict["participants"].SS.ToArray() : new string[0]
            };
        }
        
        public override string ToString()
        {
            return $"Id:{Id}, dkp:{dkpPoints}, people:{maxPeopleCount}, leaderId:{raidLeaderId} partic: {participants}";
        }
    }
}