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
	private readonly CacheService _cache;
	private readonly IdentityService _id;
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
			{ "bans", id.Bans }
		});
	}

	[HttpPost, Route("ban")]
	public ActionResult Ban2()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");
		
		string accountId = Optional<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
		string[] accounts = accountId == null
			? Require<string[]>($"{TokenInfo.FRIENDLY_KEY_ACCOUNT_ID}s")
			: new[] { accountId };
		
		Ban ban = Require<Ban>("ban");
		
		_id.Ban(ban, accounts);
		ClearCache(accountId);

		return Ok();
	}

	[HttpPatch, Route("invalidate")]
	public ActionResult Invalidate()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");

		// PLATF-6293: Admin tokens had been exposed on health checks.  In order to fix this, tokens had to be regenerated.
		// However, the old tokens are still technically valid.  Invalidating them manually would have required manual deletions
		// on multiple records in the database directly.  The updates to this endpoint allow us to do it from a single Postman request,
		// good for future-proofing in the event we leak tokens or otherwise have a security breach.
		bool all = Optional<bool>("all");								// If not specified, the next two vars are ignored / unnecessary.
		long? timestamp = Optional<long?>("timestamp");					// If specified, tokens older than this remain valid.
		bool includeAdminTokens = Optional<bool>("includeAdminTokens");	// If specified, even admin tokens will be affected.

		if (all)
		{
			long affected = _identityService.InvalidateAllTokens(includeAdminTokens, timestamp);
			Log.Warn(Owner.Default, "Tokens were invalidated!", data: new
			{
				Affected = affected,
				SpecifiedTimestamp = timestamp,
				IncludesAdminTokens = includeAdminTokens
			});
			_cache?.Clear();

			return Ok(new RumbleJson
			{
				{ "affected", affected }
			});
		}

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
		_identityService.Update(id);
		
		Log.Info(Owner.Default, "A user was unbanned.", data: new { AccountId = id.AccountId, Admin = Token });
		
		return Ok(id.ResponseObject);
	}

	private void ClearCache(string accountId)
	{
		_cache?.Clear(accountId);
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