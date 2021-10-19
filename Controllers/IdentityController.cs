using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

		[HttpPatch, Route("admin/ban")]
		public ObjectResult Ban()
		{
			return Ok();
		}

		[HttpPatch, Route("admin/invalidate")]
		public ObjectResult Invalidate()
		{
			string aid = Require<string>(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID);

			Identity identity = _identityService.Find(aid);

			return Ok();
		}

		[HttpGet, Route("validate")]
		public ObjectResult Validate() // TODO: common | EncryptedToken
		{
			return Ok(Authorization.Decode(EncryptedToken).ResponseObject);
		}

		[HttpPost, Route("generate")]
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
				IsAdmin = isAdmin
			};

			Identity identity = _identityService.Find(id);
			if (identity == null)
			{
				identity = new Identity(id, info, email);
				_identityService.Create(identity);
			}

			Authorization auth = new Authorization(info, origin, lifetime, isAdmin);
			
			identity.LatestUserInfo = info;
			identity.Tokens.Add(auth);
			_identityService.Update(identity);

			return Ok(auth.ResponseObject);
		}

		public override ActionResult HealthCheck()
		{
			return Ok(_identityService.HealthCheckResponseObject);
		}
	}
}