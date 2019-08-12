using System;
using System.Collections.Concurrent;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms
{
	internal sealed class ActivatorServiceProvider : IServiceProvider
	{
		private readonly ConcurrentDictionary<Type, ObjectFactory> factories = new ConcurrentDictionary<Type, ObjectFactory>();

		public Object GetService(Type serviceType)
		{
			IServiceProvider serviceProvider;
			if( HttpContext.Current?.Items[ ServiceHost.ServiceProviderKey ] is IServiceScope scope )
			{
				serviceProvider = scope.ServiceProvider;
			}
			else
			{
				serviceProvider = ServiceHost.GlobalServiceProvider;
			}

			// TODO: This shouldn't be necessary strictly for WebForms, but if it is being used directly outside
			// of ASP.NET internal code, they may be wanting a service directly, rather than using the ActivatorUtilities
			Object serviceInstance = null;
			try
			{
				serviceInstance = serviceProvider.GetService( serviceType );

				if( !( serviceInstance is null ) )
				{
					return serviceInstance;
				}
			}
			catch
			{
				// TODO: Find out which exceptions are thrown due to service missing and only ignore those exceptions

			}

			ObjectFactory factory = this.factories.GetOrAdd( serviceType, CreateFallbackFactory );
			return factory( serviceProvider: serviceProvider, arguments: Array.Empty<Object>() );
		}

		private static ObjectFactory CreateFallbackFactory( Type type )
		{
			try
			{
				// Create an object factory for the type.
				return ActivatorUtilities.CreateFactory( instanceType: type, argumentTypes: Array.Empty<Type>() );
			}
			catch( InvalidOperationException )
			{
				// If a proper constructor cannot be found, InvalidOperationException is thrown.
				// Most likely reason for this is not having a public constructor.  Use the
				// Activator.CreateInstance method to make use of the nonpublic default constructor
				// in this case.
				return (_sp, _arg) => Activator.CreateInstance( type, true );
			}
		}
	}
}
