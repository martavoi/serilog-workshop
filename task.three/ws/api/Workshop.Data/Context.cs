using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Workshop.Data
{
    public class Context: DbContext
    {
        private readonly string _connectionString;
        private readonly ILoggerFactory _loggerFactory;

        public Context(string connectionString, ILoggerFactory loggerFactory)
        {
            _connectionString = connectionString;
            _loggerFactory = loggerFactory;
        }
    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLoggerFactory(_loggerFactory)
                .ConfigureWarnings(warnings =>
                {
                    warnings.Log(RelationalEventId.QueryClientEvaluationWarning);
                    warnings.Log(RelationalEventId.QueryPossibleExceptionWithAggregateOperator);
                    warnings.Log(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning);
                    warnings.Ignore(CoreEventId.IncludeIgnoredWarning);
                    warnings.Ignore(CoreEventId.ContextInitialized);
                })
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