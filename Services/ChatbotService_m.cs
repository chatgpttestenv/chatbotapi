using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using BankChatbotAPI.Models;

namespace BankChatbotAPI.Services
{
    public class ChatbotService_m
    {
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<BsonDocument> _qaCollection;
        private readonly string _llmApiUrl = "http://localhost:11434/api/generate"; // Ollama or Gemma API

        public ChatbotService_m(IOptions<MongoDbSettings> mongoDbSettings)
        {
            _httpClient = new HttpClient();
            var settings = MongoClientSettings.FromUrl(new MongoUrl(mongoDbSettings.Value.ConnectionString));
            var client = new MongoClient(settings);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _qaCollection = database.GetCollection<BsonDocument>("Questions");
        }

        //    public async Task<string> GetResponseAsync(string userQuery)
        //    {
        //        // Step 1: Fetch all Q&A from MongoDB
        //        var allQuestions = await _qaCollection.Find(new BsonDocument()).ToListAsync();
        //        if (allQuestions == null || allQuestions.Count == 0)
        //        {
        //            return "No knowledge base available.";
        //        }

        //        // Step 2: Convert Q&A into a formatted string
        //        StringBuilder trainingData = new StringBuilder();
        //        trainingData.AppendLine("You are an AI chatbot trained with banking-related questions. Answer based on the given Q&A dataset.");
        //        trainingData.AppendLine("\n### Questions and Answers:\n");

        //        foreach (var doc in allQuestions)
        //        {
        //            string question = doc["question"].AsString;
        //            string answer = doc["answer"].AsString;
        //            trainingData.AppendLine($"Q: {question}\nA: {answer}\n");
        //        }

        //        // Step 3: Append the user’s query
        //        trainingData.AppendLine($"Now, answer the following user question: {userQuery}");

        //        // Step 4: Call LLM API
        //        var requestBody = new
        //        {
        //            model = "gemma:2b",
        //            prompt = trainingData.ToString(),
        //            stream = false,
        //            options = new { num_predict = 100 }
        //        };

        //        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        //        try
        //        {
        //            HttpResponseMessage response = await _httpClient.PostAsync(_llmApiUrl, jsonContent);

        //            if (!response.IsSuccessStatusCode)
        //            {
        //                return "Error fetching response from AI model.";
        //            }

        //            var responseBody = await response.Content.ReadAsStringAsync();
        //            ApiResponse jsonResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
        //            return jsonResponse.Response;
        //        }
        //        catch
        //        {
        //            return "Sorry, I am unable to process your request right now. Please try again later.";
        //        }
        //    }
        //}

        public async Task<string> GetResponseAsync(string userQuery)
        {
            // Step 1: Call Python API to get similar Q&A
            var searchRequestBody = new { query = userQuery };
            var searchContent = new StringContent(JsonConvert.SerializeObject(searchRequestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage searchResponse = await _httpClient.PostAsync("http://localhost:5000/search", searchContent);
            if (!searchResponse.IsSuccessStatusCode)
            {
                return "Error retrieving relevant Q&A.";
            }

            var searchResults = await searchResponse.Content.ReadAsStringAsync();
            var qaPairs = JsonConvert.DeserializeObject<List<QAPair>>(searchResults);

            // Step 2: Prepare context for LLM
            StringBuilder contextData = new StringBuilder();
            contextData.AppendLine("You are an AI chatbot trained for banking queries. Answer based on the following context:");

            foreach (var qa in qaPairs)
            {
                contextData.AppendLine($"Q: {qa.Question}\nA: {qa.Answer}\n");
            }

            contextData.AppendLine($"Now, answer this question: {userQuery}");

            // Step 3: Send to Gemma API
            var llmRequestBody = new
            {
                model = "gemma:2b",
                prompt = contextData.ToString(),
                stream = false,
                options = new { num_predict = 100 }
            };

            var llmContent = new StringContent(JsonConvert.SerializeObject(llmRequestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage llmResponse = await _httpClient.PostAsync(_llmApiUrl, llmContent);

            if (!llmResponse.IsSuccessStatusCode)
            {
                return "Error fetching response from AI model.";
            }

            var llmResponseBody = await llmResponse.Content.ReadAsStringAsync();
            ApiResponse jsonResponse = JsonConvert.DeserializeObject<ApiResponse>(llmResponseBody);
            return jsonResponse.Response;
        }

        // Model for deserialization
        public class QAPair
        {
            [JsonProperty("question")]
            public string Question { get; set; }

            [JsonProperty("answer")]
            public string Answer { get; set; }
        }


    }
}
