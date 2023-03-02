using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RCL.Logging;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("secured")]
public class SecuredController : TokenAuthController
{
	public const string KEY_ADMIN_SECRET = "key";
	
#pragma warning disable
	private readonly CacheService _cache;
#pragma warning restore
	
	public SecuredController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
	
	// TODO: since this is now going to be a monitored endpoint, failures like "account was banned" should not be counted
	[HttpPost, Route("token/generate"), HealthMonitor(weight: 5)]
	public ObjectResult Generate()
	{
		string wildcard = Audience.All.GetDisplayName();

		string id = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
		string screenName = Optional<string>(TokenInfo.FRIENDLY_KEY_SCREENNAME);
		int disc = Optional<int?>(TokenInfo.FRIENDLY_KEY_DISCRIMINATOR) ?? -1;
		string origin = Require<string>(Authorization.FRIENDLY_KEY_ORIGIN);
		string email = Optional<string>(Identity.FRIENDLY_KEY_EMAIL);
		long lifetime = Optional<long?>("days") ?? 5;
		string ipAddress = Optional<string>(TokenInfo.FRIENDLY_KEY_IP_ADDRESS);
		string countryCode = Optional<string>(TokenInfo.FRIENDLY_KEY_COUNTRY_CODE);
		string[] audience = Optional<string[]>(TokenInfo.FRIENDLY_KEY_AUDIENCE);

		string secret = Optional<string>(KEY_ADMIN_SECRET); // if this is present, check to see if it matches for admin access
		bool isAdmin = !string.IsNullOrWhiteSpace(secret) && secret == PlatformEnvironment.RumbleSecret;

		Identity identity = _identityService.Find(id);
		if (identity?.Banned ?? false)
			throw new AuthException(token: null, "Account was banned."); // TODO: New exception type

		if (audience == null || (audience.Length > 1 && audience.Contains(wildcard)))
			audience = new [] { wildcard };

		TokenInfo info = new TokenInfo
		{
			AccountId = id,
			Audience = audience.Distinct().OrderBy(_ => _).ToArray(),
			ScreenName = screenName,
			Discriminator = disc,
			Email = email,
			IsAdmin = isAdmin,
			Issuer = Authorization.ISSUER,
			IpAddress = ipAddress,
			GameKey = PlatformEnvironment.GameSecret,
			CountryCode = countryCode,
			Requester = origin
		};

		if (identity == null)
		{
			Log.Verbose(Owner.Default, "Token record created for account.", data: new { AccountId = id});
			identity = new Identity(id, info, email);
			_identityService.Create(identity);
		}

		Authorization auth = new Authorization(info, origin, lifetime, isAdmin);
		info.Expiration = auth.Expiration;

		identity.LatestUserInfo = info;
		identity.Authorizations.Add(auth);
		identity.Authorizations = identity.Authorizations.TakeLast(Identity.MAX_AUTHORIZATIONS_KEPT).ToList();
		if (email != null)
			identity.Email = email;
		_identityService.Update(identity);
		
		_cache?.Store(info.AccountId, true, expirationMS: TokenInfo.CACHE_EXPIRATION);

		return Ok(auth, info);
	}
}