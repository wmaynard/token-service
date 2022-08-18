using Microsoft.Extensions.DependencyInjection;
using RCL.Logging;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Filters;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace TokenService;

public class Startup : PlatformStartup
{
	// Since this service is the authority on tokens, it doesn't make sense to ping itself with web requests to validate tokens.
	// Disabling the Auth filter allows us to avoid the (minor) performance hit for checking auth validations.
	protected override PlatformOptions Configure(PlatformOptions options) => options
		.SetProjectOwner(Owner.Will)
		.SetRegistrationName("Token")
		.SetPerformanceThresholds(warnMS: 500, errorMS: 2_000, criticalMS: 30_000)
		.DisableFeatures(CommonFeature.ConsoleObjectPrinting)
		.DisableFilters(CommonFilter.Authorization)
		.DisableServices(CommonService.Config);
}