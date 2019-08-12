using System;
using System.Collections.Concurrent;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms
{
	internal class ActivatorServiceProvider : IServiceProvider
	{
		private readonly ConcurrentDictionary<Type, ObjectFactory> factories = new ConcurrentDictionary<Type, ObjectFactory>();

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
				HttpContext.Current?.Items[ ServiceHost.ServiceProviderKey ] is IServiceScope scope
				? scope.ServiceProvider
				: ServiceHost.GlobalServiceProvider
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

			var factory = this.factories.GetOrAdd(serviceType, CreateFactory);
			return factory( serviceProvider, new Object[0] );
		}
	}
}
