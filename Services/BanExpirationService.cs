using RCL.Logging;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;

namespace TokenService.Services;

public class BanExpirationService : PlatformTimerService
{
	private readonly IdentityService _identityService;
	
	public BanExpirationService(IdentityService identityService) : base(intervalMS: 5_000, startImmediately: true)
		=> _identityService = identityService;

	protected override void OnElapsed()
	{
		long count = _identityService.RemoveExpiredBans();
		if (count > 0)
			Log.Info(Owner.Will, "Removed expired bans.", data: new
			{
				Count = count
			});
	}
}