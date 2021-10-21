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
	/// TODO: Find an elegant way to remove PlatformAuthorizationFilter from this service.
	/// </summary>
	public class TokenAuthController : PlatformController
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
		public TokenAuthController(IdentityService identityService, IConfiguration config) : base(config)
		{
			_identityService = identityService;
		}

		public override ActionResult HealthCheck()
		{
			throw new System.NotImplementedException();
		}
	}
}