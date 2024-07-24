using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace TokenService.Models;

public class Identity : PlatformCollectionDocument
{
	internal const int MAX_AUTHORIZATIONS_KEPT = 10;
	internal const string GROUP_UNAUTHORIZED = "unauthorized";
	
	internal const string DB_KEY_AUTH_ATTEMPTS = "chx"; // TODO: Aliases
	internal const string DB_KEY_BANNED = "bnd";
	internal const string DB_KEY_BANS = "bans";
	internal const string DB_KEY_EMAIL = "e";
	internal const string DB_KEY_BAN_EXPIRATION = "exp";
	internal const string DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "hax";
	internal const string DB_KEY_FAILED_AUTH_ATTEMPTS = "outs";
	internal const string DB_KEY_INITIAL_USER_INFO = "iwho";
	internal const string DB_KEY_LATEST_USER_INFO = "who";
	internal const string DB_KEY_TOKENS = "tkn";
	internal const string DB_KEY_UPDATED_ON = "upd";

	public const string FRIENDLY_KEY_AUTH_ATTEMPTS = "authorizations";
	public const string FRIENDLY_KEY_BANNED = "banned";
	public const string FRIENDLY_KEY_BANS = "bans";
	public const string FRIENDLY_KEY_EMAIL = "email";
	public const string FRIENDLY_KEY_BAN_EXPIRATION = "expiration";
	public const string FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "failedAdminAuthorizations";
	public const string FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS = "failedAuthorizations";
	public const string FRIENDLY_KEY_INITIAL_USER_INFO = "initialUserInfo";
	public const string FRIENDLY_KEY_LATEST_USER_INFO = "userInfo";
	public const string FRIENDLY_KEY_TOKENS = "tokens";
	
	[BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
	[JsonInclude, JsonPropertyName(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID)]
	[SimpleIndex(unique: true)]
	public string AccountId { get; private set; }
	
	[BsonElement(DB_KEY_TOKENS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TOKENS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<Authorization> Authorizations { get; internal set; }
	
	[BsonElement(DB_KEY_BANS)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Ban[] Bans { get; internal set; }
	
	[BsonElement(DB_KEY_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long AuthAttempts { get; internal set; }
	
	// [BsonElement(DB_KEY_BANNED), BsonIgnoreIfDefault]
	// [JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANNED), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	// [CompoundIndex(group: GROUP_UNAUTHORIZED, priority: 1)]
	// public bool Banned { get; internal set; }
	
	[BsonElement(DB_KEY_EMAIL), BsonIgnoreIfNull]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EMAIL), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string Email { get; internal set; }
	
	[BsonElement(DB_KEY_BAN_EXPIRATION), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BAN_EXPIRATION), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[CompoundIndex(group: GROUP_UNAUTHORIZED, priority: 2)]
	public long BanExpiration { get; internal set; }

	[BsonElement(DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long FailedAdminAuthAttempts { get; internal set; }
	
	[BsonElement(DB_KEY_FAILED_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long FailedAuthAttempts { get; internal set; }
	
	[BsonElement(DB_KEY_UPDATED_ON)]
	[JsonIgnore]
	public long LastAccessed { get; internal set; }
	
	[BsonElement(DB_KEY_INITIAL_USER_INFO)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_INITIAL_USER_INFO)]
	public TokenInfo InitialUserInfo { get; private set; }
	
	[BsonElement(DB_KEY_LATEST_USER_INFO)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LATEST_USER_INFO)]
	public TokenInfo LatestUserInfo { get; internal set; }
	
	public long CreatedOn { get; internal set; }

	public Identity()
	{
		Bans = Array.Empty<Ban>();
		Authorizations = new List<Authorization>();
	}

	public Identity(string accountId, TokenInfo initialInfo, string email = null) : this()
	{
		AccountId = accountId;
		AuthAttempts = 0;
		FailedAuthAttempts = 0;
		FailedAdminAuthAttempts = 0;
		InitialUserInfo = initialInfo;
		Email = email;
	}

	public void Invalidate(Authorization auth) => auth.IsValid = false;
}