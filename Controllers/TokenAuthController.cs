using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Web;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers
{
	/// <summary>
	/// We can't use RequireAuth in this project; doing so would create circular references.
	/// Consequently, the Token property will never be available to controllers.
	/// This base class restores that functionality by hiding the base Token and replacing it
	/// with its own.
	/// </summary>
	public abstract class TokenAuthController : PlatformController
	{
		private const string KEY_USER_INFO = "UserInfo";
		protected readonly IdentityService _identityService;

		protected new TokenInfo Token
		{
			get
			{
				TokenInfo stored = FromContext<TokenInfo>(KEY_USER_INFO);
				if (stored != null)
					return stored;
				stored = Authorization.Decode(EncryptedToken);
				Request.HttpContext.Items[KEY_USER_INFO] = stored;
				return stored;
			}
		}
		protected TokenAuthController(IdentityService identityService, IConfiguration config) : base(config)
		{
			_identityService = identityService;
		}

		[HttpGet, Route("health")]
		public override ActionResult HealthCheck() => Ok(_identityService.HealthCheckResponseObject);
	}
}