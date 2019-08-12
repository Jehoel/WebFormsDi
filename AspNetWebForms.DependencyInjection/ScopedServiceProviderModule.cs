using System;
using System.Web;

namespace Microsoft.Extensions.DependencyInjection.WebForms
{
	internal sealed class ScopedServiceProviderModule : IHttpModule
	{
		public void Init(HttpApplication app)
		{
			app.BeginRequest += this.App_BeginRequest;
			app.EndRequest   += this.App_EndRequest;
		}

		public void Dispose()
		{
		}

		private void App_BeginRequest(Object sender, EventArgs e)
		{
			HttpContext.Current.Items.Add( ServiceHost.ServiceProviderKey, ServiceHost.GlobalServiceProvider.CreateScope() );
		}

		private void App_EndRequest(Object sender, EventArgs e)
		{
			if( HttpContext.Current.Items[ ServiceHost.ServiceProviderKey ] is IDisposable disposable ) disposable.Dispose();
		}
	}
}
