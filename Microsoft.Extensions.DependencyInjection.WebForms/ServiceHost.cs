using System;
using System.Collections.Concurrent;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms {
    public static class ServiceHost {
        private const string ServiceProviderKey = "Microsoft.Extensions.DependencyInjection:" + nameof(ServiceProviderKey);
        private static IServiceProvider _GlobalServiceProvider;

        public static void EnableServices(Action<IServiceCollection> configureServices) {
            var serviceCollection = new ServiceCollection();
            configureServices(serviceCollection);
            _GlobalServiceProvider = serviceCollection.BuildServiceProvider();
            HttpApplication.RegisterModule(typeof(ScopedServiceProviderModule));
            HttpRuntime.WebObjectActivator = new ActivatorServiceProvider();
        }

        private class ActivatorServiceProvider : IServiceProvider {
            ConcurrentDictionary<Type, ObjectFactory> _Factories = new ConcurrentDictionary<Type, ObjectFactory>();

            public object GetService(Type serviceType) {
                ObjectFactory CreateFactory(Type s) {
                    try {
                        // Create an object factory for the type.
                        return ActivatorUtilities.CreateFactory(s, new Type[0]);
                    }
                    catch (InvalidOperationException) {
                        // If a proper constructor cannot be found, InvalidOperationException is thrown.
                        // Most likely reason for this is not having a public constructor.  Use the
                        // Activator.CreateInstance method to make use of the nonpublic default constructor
                        // in this case.
                        return (_sp, _arg) => Activator.CreateInstance(s, true);
                    }
                }
                var factory = _Factories.GetOrAdd(serviceType, CreateFactory);

                var serviceProvider =
                    HttpContext.Current?.Items[ServiceProviderKey] is IServiceScope scope
                    ? scope.ServiceProvider
                    : _GlobalServiceProvider
                ;
                return factory(serviceProvider, new object[0]);
            }
        }

        private class ScopedServiceProviderModule : IHttpModule {
            public void Dispose() { }

            public void Init(HttpApplication app) {
                app.BeginRequest += App_BeginRequest;
                app.EndRequest += App_EndRequest;
                
            }

            private void App_BeginRequest(object sender, EventArgs e) {
                HttpContext.Current.Items.Add(ServiceProviderKey, _GlobalServiceProvider.CreateScope());
            }

            private void App_EndRequest(object sender, EventArgs e) {
                if (HttpContext.Current.Items[ServiceProviderKey] is IDisposable disposable) disposable.Dispose();
            }
        }

    }
}
