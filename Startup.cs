using Microsoft.Extensions.DependencyInjection;
using Rumble.Platform.Common.Filters;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace TokenService;

public class Startup : PlatformStartup
{
	public void ConfigureServices(IServiceCollection services)
	{
		// Since this service is the authority on tokens, it doesn't make sense to ping itself with web requests to validate tokens.
		// This allows us to avoid the (minor) performance hit for checking auth validations.
		BypassFilter<PlatformAuthorizationFilter>();
#if DEBUG
		base.ConfigureServices(services, defaultOwner: Owner.Will, warnMS: 5_000, errorMS: 20_000, criticalMS: 300_000);
#else
		base.ConfigureServices(services, defaultOwner: Owner.Will, warnMS: 500, errorMS: 2_000, criticalMS: 30_000);
#endif
	}
}