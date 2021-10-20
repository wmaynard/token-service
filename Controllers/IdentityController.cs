using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers
{
	[ApiController, Route("tokens")]
	public class IdentityController : TokenAuthController
	{
		public const string KEY_ADMIN_SECRET = "admin";
		public IdentityController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
		
		[HttpGet, Route("validate"), NoAuth]
		public ActionResult Validate() // TODO: common | EncryptedToken
		{
			if (Token.IsExpired)
				return Problem("expired");

			Identity id = _identityService.Find(Token.AccountId);
			if (id.Banned)
				return Problem("banned");
			
			Authorization authorization = id.Authorizations.First(auth => auth.Expiration == Token.Expiration && auth.EncryptedToken == Token.Authorization);

			if (!authorization.IsValid)
				return Problem("invalidated");

			return Ok(Token.ResponseObject);
		}

		[HttpPost, Route("generate"), NoAuth]
		public ObjectResult Generate()
		{
			string id = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);
			string screenName = Require<string>(TokenInfo.FRIENDLY_KEY_SCREENNAME);
			int disc = Require<int>(TokenInfo.FRIENDLY_KEY_DISCRIMINATOR);
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
				Issuer = Authorization.ISSUER
			};

			Identity identity = _identityService.Find(id);
			if (identity == null)
			{
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

		public override ActionResult HealthCheck()
		{
			return Ok(_identityService.HealthCheckResponseObject);
		}
	}
}