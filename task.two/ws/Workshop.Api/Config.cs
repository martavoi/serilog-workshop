using Microsoft.Extensions.Configuration;

namespace Workshop.Api
{
    public class Config
    {
        public Config(IConfiguration conf)
        {
            conf.Bind(this);
        }
    
        public string ConnectionString { get; set; }
    }
}