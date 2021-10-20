using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Web;
using TokenService.Services;

namespace TokenService.Controllers
{
	public class TopController : PlatformController
	{
		private readonly IdentityService _identityService;
		public TopController(IdentityService identityService, IConfiguration config) : base(config)
		{
			_identityService = identityService;
		}

		[HttpGet, Route("health")]
		public override ActionResult HealthCheck()
		{
			return Ok(_identityService.HealthCheckResponseObject);
		}
	}
}