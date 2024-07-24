using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("secured")]
public class SecuredController : TokenAuthController
{
	public const string KEY_ADMIN_SECRET = "key";
	private const int STANDARD_PERMISSIONS = (int)Audience.All & -(int)(
		Audience.Portal 
		| Audience.AlertService
		| Audience.DynamicConfigService
		| Audience.Portal
		| Audience.CalendarService
		| Audience.InterviewService
	);
	
#pragma warning disable
	private readonly CacheService _cache;
	private readonly IdentityService _id;
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
		int permissions = Optional<int?>(TokenInfo.FRIENDLY_KEY_PERMISSION_SET) ?? (isAdmin
			? int.MaxValue
			: STANDARD_PERMISSIONS);

		Identity identity = _id.Find(id);

		if (identity == null)
			throw new PlatformException("The identity for the given account ID is null. This should never happen.", code: ErrorCode.MongoRecordNotFound);

		if (audience == null || (audience.Length > 1 && audience.Contains(wildcard)))
			audience = new [] { wildcard };

		int bannedFeatures = identity.Bans?.Any() ?? false
			? identity
				.Bans
				.Select(ban => ban.PermissionSet)
				.Aggregate((a, b) => a | b)
			: 0;

		TokenInfo info = new TokenInfo
		{
			AccountId = id,
			ScreenName = screenName,
			Discriminator = disc,
			Email = email,
			IsAdmin = isAdmin,
			Issuer = Authorization.ISSUER,
			IpAddress = ipAddress,
			GameKey = PlatformEnvironment.GameSecret,
			CountryCode = countryCode,
			Requester = origin,
			PermissionSet = bannedFeatures > 0
				? permissions & ~bannedFeatures
				: permissions,
			Bans = identity.Bans
		};

		Authorization auth = new Authorization(info, origin, lifetime, isAdmin);
		info.Expiration = auth.Expiration;
		identity.LatestUserInfo = info;

		_id.AddAuthorization(identity, auth);
		
		_cache?.Store(info.AccountId, info, expirationMS: TokenInfo.CACHE_EXPIRATION);

		return Ok(auth, info);
	}
}