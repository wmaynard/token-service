using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Web;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers
{
	[ApiController, Route("tokens")]
	public class IdentityController : PlatformController
	{
		private readonly IdentityService _identityService;
		public IdentityController(IdentityService identityService, IConfiguration config) : base(config)
		{
			_identityService = identityService;
		}

		[HttpPost, Route("generate")]
		public ActionResult Generate()
		{
			string aid = Require<string>(Identity.FRIENDLY_KEY_ACCOUNT_ID);
			string sn = Require<string>(Identity.FRIENDLY_KEY_SCREEN_NAME);
			int disc = Require<int>(Identity.FRIENDLY_KEY_DISCRIMINATOR);
			
			// _identityService

			Authorization auth = new Authorization()
			{
				Issuer = "Rumble Token Service",
				Requester = "Ponzu",
				EncryptedToken = "eydeadbeefdeadbeefdeadbeef",
				IsAdmin = false,
				IsValid = true,
				Created = DateTimeOffset.Now.ToUnixTimeSeconds(),
				Expiration = DateTimeOffset.Now.AddDays(4).ToUnixTimeSeconds()
			};

			return Ok(auth.ResponseObject);
		}

		public override ActionResult HealthCheck()
		{
			return Ok(_identityService.HealthCheckResponseObject);
		}
	}
}