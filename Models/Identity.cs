using System.Collections.Generic;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Web;

namespace TokenService.Models
{
	public class Identity : PlatformCollectionDocument
	{
		internal const int MAX_AUTHORIZATIONS_KEPT = 10;
		
		internal const string DB_KEY_AUTH_ATTEMPTS = "chx"; // TODO: Aliases
		internal const string DB_KEY_BANNED = "bnd";
		internal const string DB_KEY_EMAIL = "e";
		internal const string DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "hax";
		internal const string DB_KEY_FAILED_AUTH_ATTEMPTS = "outs";
		internal const string DB_KEY_INITIAL_USER_INFO = "iwho";
		internal const string DB_KEY_LATEST_USER_INFO = "who";
		internal const string DB_KEY_TOKENS = "tkn";

		public const string FRIENDLY_KEY_AUTH_ATTEMPTS = "authorizations";
		public const string FRIENDLY_KEY_BANNED = "banned";
		public const string FRIENDLY_KEY_EMAIL = "email";
		public const string FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "failedAdminAuthorizations";
		public const string FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS = "failedAuthorizations";
		public const string FRIENDLY_KEY_INITIAL_USER_INFO = "initialUserInfo";
		public const string FRIENDLY_KEY_LATEST_USER_INFO = "userInfo";
		public const string FRIENDLY_KEY_TOKENS = "tokens";

		[SimpleIndex(TokenInfo.DB_KEY_ACCOUNT_ID, TokenInfo.FRIENDLY_KEY_ACCOUNT_ID)]
		[BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
		[JsonInclude, JsonPropertyName(TokenInfo.FRIENDLY_KEY_ACCOUNT_ID)]
		public string AccountId { get; private set; }
		
		[BsonElement(DB_KEY_TOKENS)]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TOKENS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<Authorization> Authorizations { get; internal set; }
		
		[BsonElement(DB_KEY_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public long AuthAttempts { get; internal set; }
		
		[BsonElement(DB_KEY_BANNED)]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_BANNED), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public bool Banned { get; internal set; }
		
		[BsonElement(DB_KEY_EMAIL)]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EMAIL), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Email { get; internal set; }
		
		[BsonElement(DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public long FailedAdminAuthAttempts { get; internal set; }
		
		[BsonElement(DB_KEY_FAILED_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public long FailedAuthAttempts { get; internal set; }
		
		[BsonElement(DB_KEY_INITIAL_USER_INFO)]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_INITIAL_USER_INFO)]
		public TokenInfo InitialUserInfo { get; private set; }
		
		[BsonElement(DB_KEY_LATEST_USER_INFO)]
		[JsonInclude, JsonPropertyName(FRIENDLY_KEY_LATEST_USER_INFO)]
		public TokenInfo LatestUserInfo { get; internal set; }
		
		public Identity(string accountId, TokenInfo initialInfo, string email = null)
		{
			AccountId = accountId;
			AuthAttempts = 0;
			FailedAuthAttempts = 0;
			FailedAdminAuthAttempts = 0;
			Authorizations = new List<Authorization>();
			InitialUserInfo = initialInfo;
			Email = email;
		}

		public void Invalidate(Authorization auth) => auth.IsValid = false;
	}
}