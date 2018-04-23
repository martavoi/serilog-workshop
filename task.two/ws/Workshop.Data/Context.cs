using Microsoft.EntityFrameworkCore;

namespace Workshop.Data
{
    public class Context: DbContext
    {
        private readonly string _connectionString;

        public Context(string connectionString)
        {
            _connectionString = connectionString;
        }
    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                //it is up to you what DB to use, but i would recommend PostgreSQL, just coz OS and free
                .UseNpgsql(_connectionString);
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new UserConfiguration());
        }
    }
}