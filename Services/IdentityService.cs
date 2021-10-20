using System.Threading.Tasks;
using MongoDB.Driver;
using Rumble.Platform.Common.Web;
using TokenService.Models;

namespace TokenService.Services
{
	public class IdentityService : PlatformMongoService<Identity>
	{
		public IdentityService() : base("identities") { }

		public Identity Find(string accountId) => _collection.Find(i => i.AccountId == accountId).FirstOrDefault();

		public void UpdateAsync(Identity id) => _collection.ReplaceOneAsync(filter: identity => identity.Id == id.Id, replacement: id);
	}
}