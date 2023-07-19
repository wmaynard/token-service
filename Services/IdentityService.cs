using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Services;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using TokenService.Models;

namespace TokenService.Services;

public class IdentityService : MinqService<Identity>
{
	public IdentityService() : base("identities") { }

	public Identity Find(string accountId) => mongo
		.Where(query => query
			.EqualTo(identity => identity.AccountId, accountId)
		)
		.FirstOrDefault();

	public Identity Find2(string accountId) => mongo
		.Where(query => query
			.EqualTo(identity => identity.AccountId, accountId)
		)
		.Upsert(query => query
			.RemoveWhere(identity => identity.Bans, filter => filter
				.LessThanOrEqualTo(ban => ban.Expiration, Timestamp.UnixTime)
			)
			.SetOnInsert(identity => identity.CreatedOn, Timestamp.UnixTime)
		);

	public long InvalidateAllPlayerTokens() => mongo
		.Where(query => query.IsNot(identity => identity.LatestUserInfo.IsAdmin))
		.Update(query => query.Set(identity => identity.Authorizations, new List<Authorization>()));

	public long InvalidateAllTokens(bool includeAdminTokens, long? timestamp) => timestamp == null
		? InvalidateAllPlayerTokens()
		: (includeAdminTokens ? mongo.All() : mongo.Where(query => query.IsNot(identity => identity.LatestUserInfo.IsAdmin)))
			.Update(query => query.RemoveWhere(
				field: identity => identity.Authorizations,
				filter => filter.LessThanOrEqualTo(auth => auth.Created, timestamp))
			);

	public void Ban(Ban ban, params string[] accounts)
	{
		Identity id = mongo
			.WithTransaction(out Transaction transaction)
			.Where(query => query.ContainedIn(identity => identity.AccountId, accounts))
			.Upsert(query => query
				.Clear(identity => identity.Authorizations)
				.AddItems(identity => identity.Bans, limitToKeep: 200, ban)
				.SetOnInsert(identity => identity.CreatedOn, Timestamp.UnixTime)
				.SetToCurrentTimestamp(identity => identity.UpdatedOn)
			);

		id.Bans = id
			.Bans
			.OrderBy(b => b.PermissionSet)
			.ThenByDescending(b => b.Expiration ?? long.MaxValue)
			.DistinctBy(b => b.PermissionSet)
			.ToArray();

		mongo
			.WithTransaction(transaction)
			.Update(id);
		
		transaction.Commit();
	}

	/// <summary>
	/// Removes a ban from an account.  Returns true if the account was modified.
	/// </summary>
	/// <param name="accountId">The account to remove a ban from.</param>
	/// <param name="banIds">One or more </param>
	/// <returns></returns>
	public bool Unban(string accountId, string[] banIds)
	{
		if (banIds == null || !banIds.Any())
			return false;
		return mongo
			.Where(query => query.EqualTo(identity => identity.AccountId, accountId))
			.Update(query => query.RemoveWhere(identity => identity.Bans, filter => filter.ContainedIn(ban => ban.Id, banIds))) > 0;
	}

	public void AddAuthorization(Identity id, Authorization jwt) => mongo
		.Where(query => query.EqualTo(identity => identity.Id, id.Id))
		.Update(query => query
			.Set(identity => identity.LatestUserInfo, id.LatestUserInfo)
			.Set(identity => identity.Email, id.Email)
			.AddItems(identity => identity.Authorizations, limitToKeep: 10, jwt)
		);
}