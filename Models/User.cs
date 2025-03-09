using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BankChatbotAPI.Models
{
    public class User
    {
        [BsonId]  // Let MongoDB automatically generate the ObjectId for the _id field
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("accountNumber")]
        public string AccountNumber { get; set; }

        [BsonElement("balance")]
        public decimal Balance { get; set; }
    }
    public class QuestionAnswer
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }
    }

    public class BotQuestion
    {
       
        public string Question { get; set; }
    }
    public class BotResponse
    {

        public string Response { get; set; }
    }
public class ApiResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("done_reason")]
        public string DoneReason { get; set; }

        [JsonProperty("context")]
        public List<int> Context { get; set; }

        [JsonProperty("total_duration")]
        public long TotalDuration { get; set; }

        [JsonProperty("load_duration")]
        public long LoadDuration { get; set; }

        [JsonProperty("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonProperty("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonProperty("eval_count")]
        public int EvalCount { get; set; }

        [JsonProperty("eval_duration")]
        public long EvalDuration { get; set; }
    }
    public class ApiResponse1
    {
        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("embedding")]
        public List<float> Embedding { get; set; }
    }

    public class QuestionAnswerPair
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }


}
