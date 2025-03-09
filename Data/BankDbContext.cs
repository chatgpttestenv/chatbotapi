using Microsoft.EntityFrameworkCore;
using BankChatbotAPI.Models;
using System.Collections.Generic;

namespace BankChatbotAPI.Data
{
    public class BankDbContext : DbContext
    {
        public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
    }
}
