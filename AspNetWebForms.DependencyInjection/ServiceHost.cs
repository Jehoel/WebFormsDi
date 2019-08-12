using System;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms
{
	public static partial class ServiceHost
	{
		internal const String ServiceProviderKey = "Microsoft.Extensions.DependencyInjection." + nameof(ServiceHost);

		internal static IServiceProvider GlobalServiceProvider { get; private set; }

		public static void EnableServices(Action<IServiceCollection> configureServices)
		{
			var serviceCollection = new ServiceCollection();
			configureServices( serviceCollection );
			GlobalServiceProvider = serviceCollection.BuildServiceProvider();
			HttpApplication.RegisterModule( typeof( ScopedServiceProviderModule ) );
			HttpRuntime.WebObjectActivator = new ActivatorServiceProvider();
		}
	}
}
