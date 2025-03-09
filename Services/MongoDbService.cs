using BankChatbotAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace BankChatbotAPI.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<QuestionAnswer> _questionAnswersCollection;

        public MongoDbService(IOptions<MongoDbSettings> mongoDbSettings)
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl(mongoDbSettings.Value.ConnectionString));
            var client = new MongoClient(settings);
            var database = client.GetDatabase(mongoDbSettings.Value.DatabaseName);
            _usersCollection = database.GetCollection<User>("Users");

            _questionAnswersCollection = database.GetCollection<QuestionAnswer>("Questions");

            // Create a unique index on the "question" field to prevent duplicates
            var indexKeysDefinition = Builders<QuestionAnswer>.IndexKeys.Ascending(q => q.Question);
            var indexOptions = new CreateIndexOptions { Unique = true };

            // Before creating the index, remove any documents where "Question" is null
            _questionAnswersCollection.DeleteMany(q => q.Question == null);

            // Now create the unique index
            _questionAnswersCollection.Indexes.CreateOne(new CreateIndexModel<QuestionAnswer>(indexKeysDefinition, indexOptions));

        }

        public async Task<User> GetUserByAccountNumberAsync(string accountNumber)
        {
            return await _usersCollection.Find(u => u.AccountNumber == accountNumber).FirstOrDefaultAsync();
        }

        public async Task AddUserAsync(User newUser)
        { 
            await _usersCollection.InsertOneAsync(newUser);
        }

            //public async Task InsertManyQuestionsAsync(List<QuestionAnswer> questionAnswers)
            //{
            //    foreach (var questionAnswer in questionAnswers)
            //    {
            //        // Check if the question already exists
            //        var existingQuestion = await _questionAnswersCollection
            //            .Find(q => q.Question == questionAnswer.Question)
            //            .FirstOrDefaultAsync();

            //        if (existingQuestion != null)
            //        {
            //            // If the question exists, you can either skip it or handle it as an error.
            //            continue; // Skip this question if it already exists
            //                      // Or throw an exception, depending on your business rules
            //                      // throw new Exception($"Question '{questionAnswer.Question}' already exists.");
            //        }
            //    }

            //    // Insert the non-duplicate question answers
            //    await _questionAnswersCollection.InsertManyAsync(questionAnswers);
            //}

        public async Task InsertManyQuestionsAsync(List<QuestionAnswer> questionAnswers)
        {
            // Filter out questions that are null or empty
            questionAnswers = questionAnswers.Where(q => !string.IsNullOrEmpty(q.Question)).ToList();

            if (questionAnswers.Count > 0)
            {
                await _questionAnswersCollection.InsertManyAsync(questionAnswers);
            }
        }

        public async Task<QuestionAnswer> GetQuestionByTextAsync(string questionText)
        {
            return await _questionAnswersCollection
                .Find(q => q.Question == questionText)
                .FirstOrDefaultAsync();
        }
    }
}
