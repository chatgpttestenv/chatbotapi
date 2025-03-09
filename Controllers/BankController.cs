using Microsoft.AspNetCore.Mvc;
using BankChatbotAPI.Services;
using System.Threading.Tasks;
using BankChatbotAPI.Models;

namespace BankChatbotAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        private readonly MongoDbService _mongoDbService;

        public BankController(MongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet("{accountNumber}")]
        public async Task<IActionResult> GetUser(string accountNumber)
        {
            var user = await _mongoDbService.GetUserByAccountNumberAsync(accountNumber);
            if (user == null) return NotFound("User not found.");
            return Ok(new { user.Name, user.Balance });
        }
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] User user)
        {
            if (user == null) return BadRequest("Invalid user data.");
            await _mongoDbService.AddUserAsync(user);
            return Ok("User added successfully.");
        }

        [HttpPost("insertquestions")]
        public async Task<IActionResult> InsertQuestions([FromBody] List<QuestionAnswer> questionAnswers)
        {
            if (questionAnswers == null || questionAnswers.Count == 0)
            {
                return BadRequest("No question-answer pairs provided.");
            }

            // Filter out questions that are null or empty
            questionAnswers = questionAnswers.Where(q => !string.IsNullOrEmpty(q.Question)).ToList();

            if (questionAnswers.Count == 0)
            {
                return BadRequest("No valid question-answer pairs provided.");
            }

            // Check if any of the questions already exist
            var existingQuestions = new List<string>();
            foreach (var questionAnswer in questionAnswers)
            {
                var existingQuestion = await _mongoDbService
                    .GetQuestionByTextAsync(questionAnswer.Question);
                if (existingQuestion != null)
                {
                    existingQuestions.Add(questionAnswer.Question);
                }
            }

            if (existingQuestions.Any())
            {
                return BadRequest($"The following questions already exist: {string.Join(", ", existingQuestions)}");
            }

            // Proceed to insert the new questions
            await _mongoDbService.InsertManyQuestionsAsync(questionAnswers);
            return Ok("Questions and answers inserted successfully.");
        }



    }
}
