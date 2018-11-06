using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.WebForms;


[assembly: WebActivatorEx.PreApplicationStartMethod(typeof($rootnamespace$.App_Start.Services), nameof($rootnamespace$.App_Start.Services.EnableServices))]

namespace $rootnamespace$.App_Start {
    public static class Services {
        public static void EnableServices() {
            ServiceHost.EnableServices(ConfigureServices);
        }

        public static void ConfigureServices(IServiceCollection services ){
            
        }
    }
}