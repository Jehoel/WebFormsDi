using System;
using System.Collections.Concurrent;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms
{
	public static class ServiceHost
	{
		private const String ServiceProviderKey = "Microsoft.Extensions.DependencyInjection:" + nameof(ServiceProviderKey);
		private static IServiceProvider _GlobalServiceProvider;

		public static void EnableServices(Action<IServiceCollection> configureServices)
		{
			var serviceCollection = new ServiceCollection();
			configureServices( serviceCollection );
			_GlobalServiceProvider = serviceCollection.BuildServiceProvider();
			HttpApplication.RegisterModule( typeof( ScopedServiceProviderModule ) );
			HttpRuntime.WebObjectActivator = new ActivatorServiceProvider();
		}

		private class ActivatorServiceProvider : IServiceProvider
		{
			private readonly ConcurrentDictionary<Type, ObjectFactory> _Factories = new ConcurrentDictionary<Type, ObjectFactory>();

			public Object GetService(Type serviceType)
			{
				ObjectFactory CreateFactory(Type s)
				{
					try
					{
						// Create an object factory for the type.
						return ActivatorUtilities.CreateFactory( s, new Type[0] );
					}
					catch( InvalidOperationException )
					{
						// If a proper constructor cannot be found, InvalidOperationException is thrown.
						// Most likely reason for this is not having a public constructor.  Use the
						// Activator.CreateInstance method to make use of the nonpublic default constructor
						// in this case.
						return (_sp, _arg) => Activator.CreateInstance( s, true );
					}
				}

				var serviceProvider =
					HttpContext.Current?.Items[ServiceProviderKey] is IServiceScope scope
					? scope.ServiceProvider
					: _GlobalServiceProvider
				;
				// TODO: This shouldn't be necessary strictly for WebForms, but if it is being used directly outside
				// of ASP.NET internal code, they may be wanting a service directly, rather than using the ActivatorUtilities
				Object serviceInstance = null;
				try
				{
					serviceInstance = serviceProvider.GetService( serviceType );
				}
				// TODO: Find out which exceptions are thrown due to service missing
				// and only ignore those exceptions
				catch { }
				if( !( serviceInstance is null ) ) return serviceInstance;

				var factory = this._Factories.GetOrAdd(serviceType, CreateFactory);
				return factory( serviceProvider, new Object[0] );
			}
		}

		private class ScopedServiceProviderModule : IHttpModule
		{
			public void Dispose() { }

			public void Init(HttpApplication app)
			{
				app.BeginRequest += this.App_BeginRequest;
				app.EndRequest += this.App_EndRequest;

			}

			private void App_BeginRequest(Object sender, EventArgs e)
			{
				HttpContext.Current.Items.Add( ServiceProviderKey, _GlobalServiceProvider.CreateScope() );
			}

			private void App_EndRequest(Object sender, EventArgs e)
			{
				if( HttpContext.Current.Items[ServiceProviderKey] is IDisposable disposable ) disposable.Dispose();
			}
		}

	}
}
