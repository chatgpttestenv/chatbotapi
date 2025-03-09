using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BankChatbotAPI.Services;
using BankChatbotAPI.Models;

namespace BankChatbotAPI.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost]
        public async Task<IActionResult> GetChatResponse([FromBody] BotQuestion request)
        {
            string question = request.Question.ToString();
            //var response = await _chatbotService.GetResponseAsync(question);
            var response = await _chatbotService.GetResponseAsync(question);
            return Ok(new { answer = response });
        }
    }
}