using System.Collections.Generic;
using System.Security.Claims;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Web;

namespace TokenService.Models
{
	// public class UserInfo : PlatformDataModel
	// {
	// 	internal const string DB_KEY_ACCOUNT_ID = "aid";
	// 	internal const string DB_KEY_SCREEN_NAME = "sn";
	// 	internal const string DB_KEY_DISCRIMINATOR = "d";
	// 	internal const string DB_KEY_EMAIL = "e";
	// 	internal const string DB_KEY_IP_ADDRESS = "ip";
	// 	
	// 	public const string FRIENDLY_KEY_ACCOUNT_ID = "accountId";
	// 	public const string FRIENDLY_KEY_SCREEN_NAME = "screenName";
	// 	public const string FRIENDLY_KEY_DISCRIMINATOR = "discriminator";
	// 	public const string FRIENDLY_KEY_EMAIL = "email";
	// 	internal const string DB_KEY_IP_ADDRESS = "ip";
	// 	public const string FRIENDLY_KEY_IP_ADDRESS = "ip";
	// 	
	// 	[BsonElement(DB_KEY_ACCOUNT_ID)]
	// 	[JsonProperty(PropertyName = FRIENDLY_KEY_ACCOUNT_ID, NullValueHandling = NullValueHandling.Include)]
	// 	public string AccountId { get; private set; }
	// 	
	// 	[BsonElement(DB_KEY_SCREEN_NAME)]
	// 	[JsonProperty(PropertyName = FRIENDLY_KEY_SCREEN_NAME, NullValueHandling = NullValueHandling.Include)]
	// 	public string ScreenName { get; private set; }
	// 	
	// 	[BsonElement(DB_KEY_DISCRIMINATOR)]
	// 	[JsonProperty(PropertyName = FRIENDLY_KEY_DISCRIMINATOR, DefaultValueHandling = DefaultValueHandling.Include)]
	// 	public int Discriminator { get; private set; }
	// 	
	// 	[BsonElement(DB_KEY_EMAIL)]
	// 	[JsonProperty(PropertyName = FRIENDLY_KEY_EMAIL, NullValueHandling = NullValueHandling.Ignore)]
	// 	public string Email { get; private set; }
	// 	
	// 	[BsonElement(DB_KEY_IP_ADDRESS), BsonIgnoreIfNull]
	// 	[JsonProperty(PropertyName = FRIENDLY_KEY_IP_ADDRESS, NullValueHandling = NullValueHandling.Ignore)]
	// 	public string IpAddress { get; private set; }
	//
	// 	[BsonConstructor]
	// 	public UserInfo()
	// 	{
	// 	}
	//
	// 	public UserInfo(string accountId, string screenName = null, int? discriminator = null, string email = null, string ipAddress = null)
	// 	{
	// 		AccountId = accountId;
	// 		ScreenName = screenName;
	// 		Discriminator = discriminator ?? 0;
	// 		Email = email;
	// 		IpAddress = ipAddress;
	// 	}
	// 	
	// 	public static UserInfo FromClaimsPrincipal(ClaimsPrincipal cp)
	// 	{
	// 		return null;
	// 	}
	// }
}