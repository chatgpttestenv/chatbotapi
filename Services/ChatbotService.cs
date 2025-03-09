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
using System.Text.RegularExpressions;

namespace BankChatbotAPI.Services
{
    public class ChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IMongoCollection<BsonDocument> _qaCollection;
        private readonly string _llmApiUrl = "http://localhost:11434/api/generate"; // Ollama or Gemma API

        public ChatbotService(IOptions<MongoDbSettings> mongoDbSettings)
        {
            _httpClient = new HttpClient();
            var settings = MongoClientSettings.FromUrl(new MongoUrl(mongoDbSettings.Value.ConnectionString));
            var client = new MongoClient(settings);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _qaCollection = database.GetCollection<BsonDocument>("Questions");
        }

        //public async Task<string> GetResponseAsync(string userQuery)
        //{
        //    // Step 1: Fetch all Q&A from MongoDB
        //    var allQuestions = await _qaCollection.Find(new BsonDocument()).ToListAsync();
        //    if (allQuestions == null || allQuestions.Count == 0)
        //    {
        //        return "No knowledge base available.";
        //    }

        //    // Step 2: Convert Q&A into a formatted string
        //    StringBuilder trainingData = new StringBuilder();
        //    trainingData.AppendLine("You are an AI chatbot trained with banking-related questions. Answer based on the given Q&A dataset.");
        //    trainingData.AppendLine("\n### Questions and Answers:\n");

        //    foreach (var doc in allQuestions)
        //    {
        //        string question = doc["question"].AsString;
        //        string answer = doc["answer"].AsString;
        //        trainingData.AppendLine($"Q: {question}\nA: {answer}\n");
        //    }

        //    // Step 3: Append the user’s query
        //    trainingData.AppendLine($"Now, answer the following user question: {userQuery}");

        //    // Step 4: Call LLM API
        //    var requestBody = new
        //    {
        //        model = "gemma:2b",
        //        prompt = trainingData.ToString(),
        //        stream = false,
        //        options = new { num_predict = 100 }
        //    };

        //    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        //    try
        //    {
        //        HttpResponseMessage response = await _httpClient.PostAsync(_llmApiUrl, jsonContent);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            return "Error fetching response from AI model.";
        //        }

        //        var responseBody = await response.Content.ReadAsStringAsync();
        //        ApiResponse jsonResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
        //        return jsonResponse.Response;
        //    }
        //    catch
        //    {
        //        return "Sorry, I am unable to process your request right now. Please try again later.";
        //    }
        //}
        public async Task<string> GetBestMatchingAnswer(string userQuery)
        {
            // Get all stored questions with embeddings
            var allQuestions = await _qaCollection.Find(new BsonDocument()).ToListAsync();

            if (!allQuestions.Any()) return "No knowledge base available.";

            // Compute embedding for user query
            var userEmbedding = await GetEmbedding(userQuery);

            string bestMatch = null;
            string bestAnswer = null;
            double highestSimilarity = -1;

            foreach (var doc in allQuestions)
            {
                if (!doc.Contains("embedding")) continue;

                var storedEmbedding = doc["embedding"].AsBsonArray.Select(v => v.AsDouble).ToArray();
                //double similarity = CosineSimilarity(userEmbedding, storedEmbedding);
                double similarity = CosineSimilarity(userEmbedding.ToList(), storedEmbedding);
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    bestMatch = doc["question"].AsString;
                    bestAnswer = doc["answer"].AsString;
                }
            }

            if (highestSimilarity > 0.75)  // If similarity > 75%, return the matched answer
            {
                return bestAnswer;
            }

            // Otherwise, generate response using Gemma
            return await GenerateResponseAsync($"You are a banking chatbot. Answer this: {userQuery}");
        }
        public async Task<float[]> GetEmbedding(string text)
        {
            try
            {
                var requestBody = new
                {
                    model = "gemma:2b",
                    prompt = text,
                    stream = false,
                    options = new { num_predict = 250 }
                }; // Adjust based on the API's expected request format
                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync(_llmApiUrl, jsonContent);
                response.EnsureSuccessStatusCode(); // Throws an exception if the request failed

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Raw API Response: " + responseBody); // Log response for debugging

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    JsonElement root = doc.RootElement;

                    // Check if 'response' field exists and contains embeddings
                    if (root.TryGetProperty("response", out JsonElement responseElement))
                    {
                        string responseString = responseElement.GetString();

                        // If response is a stringified JSON, parse it again
                        using (JsonDocument nestedDoc = JsonDocument.Parse(responseString))
                        {
                            JsonElement nestedRoot = nestedDoc.RootElement;
                            if (nestedRoot.TryGetProperty("embeddings", out JsonElement embeddingsElement))
                            {
                                return JsonConvert.DeserializeObject<float[]>(embeddingsElement.GetRawText());
                            }
                        }
                    }
                    else if (root.TryGetProperty("embeddings", out JsonElement embeddingsElement))
                    {
                        return JsonConvert.DeserializeObject<float[]>(embeddingsElement.GetRawText());
                    }
                }

                throw new Exception("Embeddings not found in response.");
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
                throw new Exception("Failed to fetch embeddings from the API.");
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Console.WriteLine($"JSON Parsing Error: {jsonEx.Message}");
                throw new Exception("Failed to parse the embeddings response.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                throw new Exception("Failed to extract embeddings from the response.");
            }
        }
        private List<float> ExtractEmbeddings(string responseText)
        {
            // Find the part of the response that contains numerical values
            string pattern = @"\{\s*""user_type""\s*:\s*(\d+),\s*""transaction_type""\s*:\s*(\d+),\s*""time_of_transaction""\s*:\s*\{.*?\}\s*\}";

            var match = Regex.Match(responseText, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                return null;
            }

            List<float> embeddings = new List<float>();

            // Extract numerical values from the match groups
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (float.TryParse(match.Groups[i].Value, out float value))
                {
                    embeddings.Add(value);
                }
            }

            return embeddings;
        }

        private double CosineSimilarity(List<float> vectorA, double[] vectorB)
        {
            double dotProduct = 0, magA = 0, magB = 0;
            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magA += Math.Pow(vectorA[i], 2);
                magB += Math.Pow(vectorB[i], 2);
            }
            return dotProduct / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }

        public async Task<string> GetResponseAsync(string userQuery)
        {
            // Step 1: Search for the most relevant Q&A in MongoDB
            //var pythonEmbeddingService = new PythonEmbeddingService();
            //List<float> userEmbedding = pythonEmbeddingService.GetEmbedding(userQuery);
            var filter = Builders<BsonDocument>.Filter.Regex("question", new BsonRegularExpression(userQuery, "i"));
            var matchedQuestion = await _qaCollection.Find(filter).FirstOrDefaultAsync();

            if (matchedQuestion == null)
            {
                StringBuilder bottrainingData = new StringBuilder();
                bottrainingData.AppendLine($"You are an AI chatbot trained with banking-related questions. Also don't answer outside the bankin domain.");
                bottrainingData.AppendLine($"Now, answer the following user question in short maximum 100 words: {userQuery}");
                var botresponse = await GenerateResponseAsync(bottrainingData.ToString());
                return botresponse;
            }

            // Step 2: Convert Q&A into a formatted string
            StringBuilder trainingData = new StringBuilder();
            trainingData.AppendLine("You are an AI chatbot trained with banking-related questions. Answer based on the given Q&A dataset.please dont answer outside the bankin domain");
            trainingData.AppendLine("\n### Questions and Answers:\n");

            foreach (var doc in matchedQuestion)
            {
                string question = matchedQuestion["question"].AsString;
                string answer = matchedQuestion["answer"].AsString;
                trainingData.AppendLine($"Q: {question}\nA: {answer}\n");
            }

            // Step 3: Append the user’s query
            trainingData.AppendLine($"Now, answer the following user question: {userQuery}");

            //string bestQuestion = matchedQuestion["question"].AsString;
            //string bestAnswer = matchedQuestion["answer"].AsString;

            // Step 2: Generate a refined response using LLM
            var response = await GenerateResponseAsync(trainingData.ToString());
            return response;

        }

        private async Task<string> GenerateResponseAsync(string trainingData)
        {
            // Step 4: Call LLM API
            var requestBody = new
            {
                model = "gemma:2b",
                prompt = trainingData,
                stream = false,
                options = new { num_predict = 250 }
            };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_llmApiUrl, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    return "Error fetching response from AI model.";
                }
                var responseBody = await response.Content.ReadAsStringAsync();
                ApiResponse jsonResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);
                return jsonResponse.Response;
            }
            catch
            {
                return "Sorry, I am unable to process your request right now. Please try again later.";
            }
        }

    }
}
