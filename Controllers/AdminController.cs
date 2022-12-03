using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("token/admin")]
public class AdminController : TokenAuthController
{
#pragma warning disable
	private readonly ApiService _apiService;
	private readonly DynamicConfig _dynamicConfig;
	// private readonly DynamicConfigService _config;
#pragma warning restore
	
	public AdminController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }

	[HttpGet, Route("status")]
	public ActionResult ViewStatus()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");

		Identity id = _identityService.Find(Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID));

		return Ok(new RumbleJson
		{
			{ "banned", id.Banned },
			{ "expiration", id.BanExpiration }
		});
	}
	
	[HttpPatch, Route("ban")]
	public ActionResult Ban()
	{
		// TODO: Optional<long>("duration")?
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");

		string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
		long? durationInSeconds = Optional<long?>("duration");
		
		Identity id = _identityService.Find(accountId);
		if (id.Banned)
			Log.Warn(Owner.Default, "Tried to ban an already-banned user.  Using the longer of the two durations.", data: new { AccountId = id.AccountId, Admin = Token });
		id.Banned = true;
		id.BanExpiration = durationInSeconds != null
			? Math.Max(id.BanExpiration, Timestamp.UnixTime + (long)durationInSeconds)
			: default;
		_identityService.Update(id);
		
		ClearCache(accountId);
		
		Log.Info(Owner.Default, "A user was banned.", data: new { AccountId = id.AccountId, Admin = Token });
		
		return Ok(id.ResponseObject);
	}

	[HttpPatch, Route("invalidate")]
	public ActionResult Invalidate()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");

		string accountId = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
		Identity id = _identityService.Find(accountId);
		foreach (Authorization auth in id.Authorizations)
			auth.IsValid = false;
		_identityService.Update(id);

		ClearCache(accountId);

		Log.Info(Owner.Default, "A user's active sessions have been invalidated.", data: new { AccountId = id.AccountId, Admin = Token });

		return Ok(id.ResponseObject);
	}
	
	[HttpPatch, Route("unban")]
	public ActionResult Unban()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");
		Identity id = _identityService.Find(Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID));
		if (!id.Banned)
			Log.Warn(Owner.Default, "Tried to unban a user that wasn't banned.", data: new { AccountId = id.AccountId, Admin = Token});
		id.Banned = false;
		_identityService.Update(id);
		
		Log.Info(Owner.Default, "A user was unbanned.", data: new { AccountId = id.AccountId, Admin = Token });
		
		return Ok(id.ResponseObject);
	}

	private void ClearCache(string accountId)
	{
		// TODO: Use DC2 to invalidate tokens on every service container
		if (_dynamicConfig == null)
			throw new PlatformException("Dynamic config is null.");
		if (_dynamicConfig.ProjectValues == null)
		{
			Log.Warn(Owner.Will, "Dynamic config is missing values.", data: new
			{
				k = _dynamicConfig.AllValues.Select(pair => pair.Key).OrderBy(_ => _)
			});
			throw new PlatformException("Dynamic config's GameConfig section is null.");
		}

		string url = PlatformEnvironment.Url("player/v2/cachedToken");
		string adminToken = _dynamicConfig.AdminToken;

		for (int i = 0; i < 10; i++) // TODO: This is a kluge until we get the DC2 functionality in
			_apiService
				.Request(url)
				.AddAuthorization(adminToken)
				.AddParameter("accountId", accountId)
				.OnFailure((sender, apiResponse) =>
				{
					Log.Local(Owner.Will, $"Unable to clear the token cache! {url} {(int)apiResponse}");
				})
				.Delete(out RumbleJson response, out int code);
	}
}