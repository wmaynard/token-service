using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Web;

namespace TokenService.Controllers
{
	public class TopController : PlatformController
	{
		public TopController(IConfiguration config) : base(config) { }

		public override ActionResult HealthCheck()
		{
			return Ok();
		}
	}
}