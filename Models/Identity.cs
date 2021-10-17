using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace TokenService.Models
{
	public class Identity : PlatformCollectionDocument
	{
		internal const string DB_KEY_ACCOUNT_ID = "aid";
		internal const string DB_KEY_AUTH_ATTEMPTS = "chx";
		internal const string DB_KEY_FAILED_AUTH_ATTEMPTS = "outs";
		internal const string DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "hax";
		internal const string DB_KEY_TOKENS = "tkn";

		public const string FRIENDLY_KEY_ACCOUNT_ID = "AccountId";
		public const string FRIENDLY_KEY_AUTH_ATTEMPTS = "Authorizations";
		public const string FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS = "FailedAuthorizations";
		public const string FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS = "FailedAdminAuthorizations";
		public const string FRIENDLY_KEY_SCREEN_NAME = "ScreenName";
		public const string FRIENDLY_KEY_DISCRIMINATOR = "Discriminator";
		public const string FRIENDLY_KEY_TOKENS = "Tokens";
		
		[BsonElement(DB_KEY_ACCOUNT_ID)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_ACCOUNT_ID, NullValueHandling = NullValueHandling.Include)]
		public string AccountId { get; private set; }
		[BsonElement(DB_KEY_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(PropertyName = FRIENDLY_KEY_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long AuthAttempts { get; private set; }
		[BsonElement(DB_KEY_FAILED_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(PropertyName = FRIENDLY_KEY_FAILED_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long FailedAuthAttempts { get; private set; }
		[BsonElement(DB_KEY_FAILED_ADMIN_AUTH_ATTEMPTS), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_FAILED_ADMIN_AUTH_ATTEMPTS, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long FailedAdminAuthAttempts { get; private set; }
		[BsonElement(DB_KEY_TOKENS)]
		[JsonProperty(PropertyName = FRIENDLY_KEY_TOKENS, NullValueHandling = NullValueHandling.Ignore)]
		public Authorization[] Tokens { get; private set; }
		
		[BsonIgnore]
		[JsonProperty(FRIENDLY_KEY_SCREEN_NAME, NullValueHandling = NullValueHandling.Include)]
		public string ScreenName { get; private set; } // decrypt most recent auth
		[BsonIgnore]
		[JsonProperty(FRIENDLY_KEY_DISCRIMINATOR, NullValueHandling = NullValueHandling.Include)]
		public int Discriminator { get; private set; } // decrypt most recent auth
	}
}