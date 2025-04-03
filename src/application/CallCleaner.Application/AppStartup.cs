using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CallCleaner.Application
{
    public class AppStartup
    {
        protected readonly IHostEnvironment _hostEnvironment;
        protected readonly IConfiguration _configuration;

        public AppStartup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            _configuration = configuration;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Bu metod runtime'da çağırılır. Container'a servis eklemek için kullanılır.
        /// Tüm configurasyonlarda çağırılması gerekir.
        /// </summary>
        /// <param name="services"></param>

        //public virtual void ConfigureServices(IServiceCollection services)
        //{
        //    //services.AddJwtConfigure(_configuration);

        //    //services.AddDependencyResolvers(
        //    //    _configuration,
        //    //    new ICoreModule[] {
        //    //        new CoreModule(),
        //    //        new AppModule()
        //    //    });

        //    services.AddDbContext<DataContext>(opt => opt
        //            .UseNpgsql(_configuration.GetConnectionString("Default"))
        //            //.UseSnakeCaseNamingConvention()
        //            .EnableSensitiveDataLogging());
        //}
    }
}
