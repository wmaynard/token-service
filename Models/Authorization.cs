using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace TokenService.Models
{
	public class Authorization : PlatformDataModel
	{
		internal const string DB_KEY_ISSUER = "iss";
		internal const string DB_KEY_REQUESTER = "req";
		internal const string DB_KEY_EXPIRATION = "xp";
		internal const string DB_KEY_TOKEN = "val";
		internal const string DB_KEY_IS_ADMIN = "su";
		internal const string DB_KEY_IS_VALID = "ok";
		internal const string DB_KEY_CREATED = "ts";

		public const string FRIENDLY_KEY_ISSUER = "issuer";
		public const string FRIENDLY_KEY_REQUESTER = "origin";
		public const string FRIENDLY_KEY_TOKEN = "token";
		public const string FRIENDLY_KEY_IS_ADMIN = "isAdmin";
		public const string FRIENDLY_KEY_IS_VALID = "isValid";
		public const string FRIENDLY_KEY_EXPIRATION = "expiration";
		public const string FRIENDLY_KEY_CREATED = "created";
		public const string FRIENDLY_KEY_REMAINING = "secondsRemaining";
		
		[BsonElement(DB_KEY_ISSUER)]
		[JsonProperty(FRIENDLY_KEY_ISSUER, NullValueHandling = NullValueHandling.Include)]
		public string Issuer { get; private set; }
		[BsonElement(DB_KEY_REQUESTER)]
		[JsonProperty(FRIENDLY_KEY_REQUESTER, NullValueHandling = NullValueHandling.Include)]
		public string Requester { get; private set; }
		[BsonElement(DB_KEY_TOKEN)]
		[JsonProperty(FRIENDLY_KEY_TOKEN, NullValueHandling = NullValueHandling.Include)]
		public string EncryptedToken { get; private set; }
		
		[BsonElement(DB_KEY_IS_ADMIN), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_IS_ADMIN, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsAdmin { get; private set; }
		[BsonElement(DB_KEY_IS_VALID)]
		[JsonProperty(FRIENDLY_KEY_IS_VALID, DefaultValueHandling = DefaultValueHandling.Include)]
		public bool IsValid { get; private set; }
		[BsonElement(DB_KEY_EXPIRATION)]
		[JsonProperty(FRIENDLY_KEY_EXPIRATION, DefaultValueHandling = DefaultValueHandling.Include)]
		public long Expiration { get; private set; }
		[BsonElement(DB_KEY_CREATED), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_CREATED, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long Created { get; private set; }
		
		[BsonIgnore]
		[JsonIgnore]
		public long SecondsRemaining => Expiration - Created;
		
		
		// public string RemoteAddress { get; private set; }
		// public string GeoIpAddress { get; private set; }
		// public string Country { get; private set; }

		// remoteAddress, geoipAddress, country, serverTime, accountId, requestId?, token
	}
}