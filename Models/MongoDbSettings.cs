namespace BankChatbotAPI.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string QwenModelAPI { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
    }
}
