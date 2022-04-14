using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("token/admin")]
public class AdminController : TokenAuthController
{
	public AdminController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
	
	[HttpPatch, Route("ban")]
	public ActionResult Ban()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");
		
		Identity id = _identityService.Find(Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID));
		if (id.Banned)
			Log.Warn(Owner.Default, "Tried to ban an already-banned user.", data: new { AccountId = id.AccountId, Admin = Token });
		id.Banned = true;
		_identityService.Update(id);
		
		Log.Info(Owner.Default, "A user was banned.", data: new { AccountId = id.AccountId, Admin = Token });
		
		return Ok(id.ResponseObject);
	}

	[HttpPatch, Route("invalidate")]
	public ActionResult Invalidate()
	{
		if (!Token.IsAdmin)
			throw new AuthException(Token, "Admin privileges required.");

		Identity id = _identityService.Find(Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID));
		foreach (Authorization auth in id.Authorizations)
			auth.IsValid = false;
		_identityService.Update(id);
		
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
}