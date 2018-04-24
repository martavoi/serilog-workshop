using Microsoft.EntityFrameworkCore.Design;

namespace Workshop.Data
{
    public class DesignTimeContextFactory: IDesignTimeDbContextFactory<Context>
    {
        public Context CreateDbContext(string[] args)
        {
#if DEBUG
            return new Context("Host=localhost;Username=postgres;Password=12345;Database=Users", null); 
#else
        throw new System.InvalidOperationException();
#endif
        }
    }
}