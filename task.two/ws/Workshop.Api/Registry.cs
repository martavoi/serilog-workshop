namespace Workshop.Api
{
    public class Registry: StructureMap.Registry
    {
        public Registry(Config conf)
        {   
            For<Data.Context>().Use<Data.Context>()
                            .Ctor<string>().Is(conf.ConnectionString)
                            .ContainerScoped();
                        For<Data.IUsersRepository>().Use<Data.UsersRepository>().Transient();
        }
    }
}