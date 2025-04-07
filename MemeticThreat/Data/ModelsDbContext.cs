using Microsoft.EntityFrameworkCore;
using MemeticThreatServerAPI.Models;

namespace MemeticThreatServerAPI.Data
{
    public class ModelsDbContext : DbContext
    {
        public DbSet<FileModel> FileModels { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        public ModelsDbContext(DbContextOptions<ModelsDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }
    }
}
