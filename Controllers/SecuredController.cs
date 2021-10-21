using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers
{
	[ApiController, Route("secured")]
	public class SecuredController : TokenAuthController
	{
		public const string KEY_ADMIN_SECRET = "key";
		public SecuredController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
		
		[HttpPost, Route("token/generate")]
		public ObjectResult Generate()
		{
			string id = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
			string screenName = Optional<string>(TokenInfo.FRIENDLY_KEY_SCREENNAME);
			int disc = Optional<int?>(TokenInfo.FRIENDLY_KEY_DISCRIMINATOR) ?? -1;
			string origin = Require<string>(Authorization.FRIENDLY_KEY_ORIGIN);
			string email = Optional<string>(Identity.FRIENDLY_KEY_EMAIL);
			long lifetime = Optional<long>("days");

			string secret = Optional<string>(KEY_ADMIN_SECRET); // if this is present, check to see if it matches for admin access
			bool isAdmin = !string.IsNullOrWhiteSpace(secret) && secret == Authorization.ADMIN_SECRET;
			
			TokenInfo info = new TokenInfo()
			{
				AccountId = id,
				ScreenName = screenName,
				Discriminator = disc,
				IsAdmin = isAdmin,
				Issuer = Authorization.ISSUER,
				IpAddress = IpAddress
			};

			Identity identity = _identityService.Find(id);
			if (identity == null)
			{
				Log.Info(Owner.Default, "Token record created for account.", data: new { AccountId = id});
				identity = new Identity(id, info, email);
				_identityService.Create(identity);
			}

			Authorization auth = new Authorization(info, origin, lifetime, isAdmin);
			info.Expiration = auth.Expiration;

			identity.LatestUserInfo = info;
			identity.Authorizations.Add(auth);
			identity.Authorizations = identity.Authorizations.TakeLast(Identity.MAX_AUTHORIZATIONS_KEPT).ToList();
			_identityService.Update(identity);

			return Ok(auth.ResponseObject, info.ResponseObject);
		}
	}
}