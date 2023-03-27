using System.Collections.Generic;
using System.Linq;
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

	public long InvalidateAllPlayerTokens() => _collection.UpdateMany(
		filter: Builders<Identity>.Filter.Eq(identity => identity.LatestUserInfo.IsAdmin, false),
		update: Builders<Identity>.Update.Set(identity => identity.Authorizations, new List<Authorization>())
	).ModifiedCount;

	public long InvalidateAllTokens(bool includeAdminTokens, long? timestamp) => timestamp == null
		? InvalidateAllPlayerTokens()
		: _collection.UpdateMany(
			filter: includeAdminTokens
				? _ => true
				: identity => !identity.LatestUserInfo.IsAdmin,
			update: Builders<Identity>.Update.PullFilter(identity => identity.Authorizations, auth => auth.Created <= timestamp)
		).ModifiedCount;
}