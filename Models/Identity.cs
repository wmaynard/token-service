using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rumble.Platform.Common.Web;

namespace TokenService.Models
{
	public class Identity : PlatformCollectionDocument
	{
		internal const int MAX_AUTHORIZATIONS_KEPT = 10;
		
		internal const string DB_KEY_AUTH_ATTEMPTS = "chx"; // TODO: Aliases
		internal const string DB_KEY_EMAIL = "e";
		internal const string DB_KEY_FAILED_AUTH_ATTEMPTS = "outs";
		internal const string DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "hax";
		internal const string DB_KEY_TOKENS = "tkn";
		internal const string DB_KEY_INITIAL_USER_INFO = "iwho";
		internal const string DB_KEY_LATEST_USER_INFO = "who";
		internal const string DB_KEY_BANNED = "bnd";

		public const string FRIENDLY_KEY_AUTH_ATTEMPTS = "authorizations";
		public const string FRIENDLY_KEY_EMAIL = "email";
		public const string FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS = "failedAuthorizations";
		public const string FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "failedAdminAuthorizations";
		public const string FRIENDLY_KEY_TOKENS = "tokens";
		public const string FRIENDLY_KEY_INITIAL_USER_INFO = "initialUserInfo";
		public const string FRIENDLY_KEY_LATEST_USER_INFO = "userInfo";
		public const string FRIENDLY_KEY_BANNED = "banned";

		[BsonElement(TokenInfo.DB_KEY_ACCOUNT_ID)]
		[JsonProperty(PropertyName = TokenInfo.FRIENDLY_KEY_ACCOUNT_ID, NullValueHandling = NullValueHandling.Include)]
		public string AccountId { get; private set; }
		[BsonElement(DB_KEY_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(PropertyName = FRIENDLY_KEY_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long AuthAttempts { get; internal set; }
		[BsonElement(DB_KEY_FAILED_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(PropertyName = FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long FailedAuthAttempts { get; internal set; }
		[BsonElement(DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long FailedAdminAuthAttempts { get; internal set; }
		[BsonElement(DB_KEY_TOKENS)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_TOKENS, NullValueHandling = NullValueHandling.Ignore)]
		public List<Authorization> Authorizations { get; internal set; }
		
		[BsonElement(DB_KEY_LATEST_USER_INFO)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_LATEST_USER_INFO, NullValueHandling = NullValueHandling.Include)]
		public TokenInfo LatestUserInfo { get; internal set; }
		[BsonElement(DB_KEY_INITIAL_USER_INFO)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_INITIAL_USER_INFO, NullValueHandling = NullValueHandling.Include)]
		public TokenInfo InitialUserInfo { get; private set; }
		
		[BsonElement(DB_KEY_EMAIL)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_EMAIL, NullValueHandling = NullValueHandling.Ignore)]
		public string Email { get; private set; }
		[BsonElement(DB_KEY_BANNED)]
		[JsonProperty(FRIENDLY_KEY_BANNED, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool Banned { get; internal set; }

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

		public void Invalidate(Authorization auth)
		{
			auth.IsValid = false;
		}
	}
}