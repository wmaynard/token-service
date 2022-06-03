using MongoDB.Driver;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Models;

namespace TokenService.Services;

public class IdentityService : PlatformMongoService<Identity>
{
	public IdentityService() : base("identities") { }

	public Identity Find(string accountId) => _collection.Find(i => i.AccountId == accountId).FirstOrDefault();

	public void UpdateAsync(Identity id) => _collection.ReplaceOneAsync(filter: identity => identity.Id == id.Id, replacement: id);

	public long RemoveExpiredBans() => _collection.UpdateMany(
		filter: identity => identity.Banned && identity.BanExpiration <= Timestamp.UnixTime,
		update: Builders<Identity>.Update.Combine(updates: new []
		{
			Builders<Identity>.Update.Set(identity => identity.Banned, false),
			Builders<Identity>.Update.Set(identity => identity.BanExpiration, default)
		})
	).ModifiedCount;
}