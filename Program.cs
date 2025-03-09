using BankChatbotAPI.Models;
using BankChatbotAPI.Services;
using Microsoft.Extensions.Options;
using Python.Runtime;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<ChatbotService>();
Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", @"C:\Users\Ranoo\AppData\Local\Programs\Python\Python311\python311.dll");
PythonEngine.PythonHome = @"C:\Users\Ranoo\AppData\Local\Programs\Python\Python311\";  // Set this to your Python path
//PythonEngine.PythonDLL = @"C:\Program Files\Python311\python311.dll";  // Ensure this file exists
PythonEngine.Initialize();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
var app = builder.Build();
app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();



app.Run();
