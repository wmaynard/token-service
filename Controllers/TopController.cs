using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Data;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("token")]
public class TopController : TokenAuthController
{
#pragma warning disable
	private readonly CacheService _cache;
#pragma warning restore
	
	public TopController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
	
	[HttpGet, Route("validate")]
	public ActionResult Validate() // TODO: common | EncryptedToken
	{
		string origin = Require<string>("origin");
		string endpoint = Optional<string>("endpoint");

		if (_cache?.HasValue(Token.AccountId, out TokenInfo cached) ?? false)
			return Ok(new RumbleJson
			{
				{ TokenInfo.KEY_TOKEN_OUTPUT, cached },
				{ TokenInfo.KEY_TOKEN_LEGACY_OUTPUT, cached }
			});
		
		if (Token.IsExpired)
			throw new AuthException(Token, origin, endpoint, "Token has expired.");

		Identity id = _identityService.Find2(Token.AccountId);
		Authorization authorization = id.Authorizations.FirstOrDefault(auth => auth.Expiration == Token.Expiration && auth.EncryptedToken == Token.Authorization);
		
		if (authorization == null)
			throw new AuthException(Token, origin, endpoint, "Token has expired, been replaced, or been invalidated.");
		if (!authorization.IsValid)
			throw new AuthException(Token, origin, endpoint, "Token was invalidated.");

		Token.Bans = id.Bans;
		
		_cache?.Store(Token.AccountId, Token, expirationMS: TokenInfo.CACHE_EXPIRATION);
		
		Graphite.Track(Token.IsAdmin
				? "admin-tokens-validated"
				: "tokens-validated", 1
		);

		return Ok(new RumbleJson
		{
			{ TokenInfo.KEY_TOKEN_OUTPUT, Token },
			{ TokenInfo.KEY_TOKEN_LEGACY_OUTPUT, Token } // deprecated; legacy key
		});
	}
}