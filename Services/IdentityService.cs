using Rumble.Platform.Common.Web;
using TokenService.Models;

namespace TokenService.Services
{
	public class IdentityService : PlatformMongoService<Identity>
	{
		public IdentityService() : base("identities") { }
	}
}