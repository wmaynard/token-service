using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Jose;
using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Attributes;
using Rumble.Platform.Common.Enums;
using Rumble.Platform.Common.Extensions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;
using TokenService.Exceptions;

namespace TokenService.Models;

public class Authorization : PlatformDataModel
{
	internal const string ISSUER = "Rumble Token Service";
	// internal static readonly string AUDIENCE = PlatformEnvironment.Require<string>("GAME_KEY");

	// private const string CLAIM_KEY_ISSUED_AT = "iat";
	// private const string CLAIM_KEY_AUDIENCE = "aud";
	
	internal const string DB_KEY_CREATED = "ts";
	internal const string DB_KEY_EXPIRATION = "xp";
	internal const string DB_KEY_IS_ADMIN = "su";
	internal const string DB_KEY_IS_VALID = "ok";
	internal const string DB_KEY_ISSUER = "iss";
	internal const string DB_KEY_ORIGIN = "org";
	internal const string DB_KEY_TOKEN = "val";

	public const string FRIENDLY_KEY_CREATED = "created";
	public const string FRIENDLY_KEY_EXPIRATION = "expiration";
	public const string FRIENDLY_KEY_IS_ADMIN = "isAdmin";
	public const string FRIENDLY_KEY_IS_VALID = "isValid";
	public const string FRIENDLY_KEY_ISSUER = "issuer";
	public const string FRIENDLY_KEY_ORIGIN = "origin";
	public const string FRIENDLY_KEY_TOKEN = "token";
	
	[BsonElement(DB_KEY_CREATED), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_CREATED), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long Created { get; internal set; }
	
	[SimpleIndex]
	[BsonElement(DB_KEY_TOKEN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_TOKEN)]
	public string EncryptedToken { get; internal set; }
	
	[SimpleIndex]
	[BsonElement(DB_KEY_EXPIRATION)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_EXPIRATION)]
	public long Expiration { get; internal set; }
	
	[BsonElement(DB_KEY_IS_ADMIN), BsonIgnoreIfDefault]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_ADMIN), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool IsAdmin { get; private set; }
	
	[BsonElement(DB_KEY_IS_VALID)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_IS_VALID)]
	public bool IsValid { get; internal set; }
	
	[BsonElement(DB_KEY_ISSUER)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ISSUER)]
	public string Issuer { get; private set; }
	
	[BsonElement(DB_KEY_ORIGIN)]
	[JsonInclude, JsonPropertyName(FRIENDLY_KEY_ORIGIN)]
	public string Origin { get; internal set; }
	
	[BsonIgnore]
	[JsonIgnore]
	public long SecondsRemaining => Expiration - Created;
	
	// TODO: Privatize

	public Authorization(TokenInfo info, string origin, long lifetimeDays = 4, bool isAdmin = false)
	{
		Issuer = ISSUER;
		IsAdmin = isAdmin;
		IsValid = true;
		
		Origin = origin;

		lifetimeDays = IsAdmin || PlatformEnvironment.IsLocal
			? Math.Min(3650, lifetimeDays) // Maximum lifetime of 10 years.
			: Math.Min(5, lifetimeDays); // Maximum lifetime of 5 days.
		
		DateTimeOffset now = DateTimeOffset.Now;
		Created = now.ToUnixTimeSeconds();
		try
		{
			Expiration = now.AddDays(lifetimeDays).ToUnixTimeSeconds();
		}
		catch (ArgumentOutOfRangeException)
		{
			Log.Warn(Owner.Default, "A token was generated that effectively doesn't expire.", data: new
			{
				User = info,
				Origin = origin
			});
			Expiration = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
		}

		// When Mongo is trying to automatically register models, info will be null.  Prevent the exception with a null-check for now.
		// TODO: clean this up
		if (info == null)
			return;

		info.Expiration = Expiration;
		info.IsAdmin = isAdmin;
		
		EncryptedToken = GenerateToken(info);
	}

	/// <summary>
	/// Decodes and validates an encrypted token.
	/// </summary>
	/// <param name="token">The token to decode.</param>
	/// <param name="requestingService">The ServiceName of the requesting entity.</param>
	/// <returns>Information that was embedded in the token.</returns>
	internal static TokenInfo Decode(string token, string requestingService)
	{
		token = token?.Replace("Bearer ", "");
		if (string.IsNullOrWhiteSpace(token))
			throw new AuthException(token: null, "No token provided.");
		try
		{
			Dictionary<string, object> claims = JsonWebToken.Decode(token);

			claims.TryGetValue(TokenInfo.DB_KEY_ACCOUNT_ID, out object aid);
			claims.TryGetValue(TokenInfo.DB_KEY_SCREENNAME, out object sn);
			claims.TryGetValue(TokenInfo.DB_KEY_DISCRIMINATOR, out object disc);
			claims.TryGetValue(TokenInfo.DB_KEY_IS_ADMIN, out object admin);
			claims.TryGetValue(TokenInfo.DB_KEY_EMAIL_ADDRESS, out object email);
			claims.TryGetValue(TokenInfo.DB_KEY_EXPIRATION, out object expiration);
			claims.TryGetValue(TokenInfo.DB_KEY_ISSUER, out object issuer);
			claims.TryGetValue(TokenInfo.DB_KEY_IP_ADDRESS, out object ip);
			claims.TryGetValue(TokenInfo.DB_KEY_COUNTRY_CODE, out object countryCode);
			claims.TryGetValue(TokenInfo.DB_KEY_AUDIENCE, out object audienceObject);
			claims.TryGetValue(TokenInfo.DB_KEY_REQUESTER, out object requester);
			claims.TryGetValue(TokenInfo.DB_KEY_ISSUED_AT, out object issuedAt);
			claims.TryGetValue(TokenInfo.DB_KEY_GAME, out object gameKey);
			claims.TryGetValue(TokenInfo.DB_KEY_PERMISSION_SET, out object perms);

			
			try
			{
				// Backwards compatibility for tokens generated prior to Bans V2.  Old tokens come in with an audience
				// array.  If we don't see the new permissions key and this is specified, translate it into the new
				// permissions.
				string[] audience = ((object[])audienceObject)
					.Select(obj => (string)obj)
					.ToArray();
				if (audience.Any())
					perms ??= (int)Enum.GetValues<Audience>()
						.Where(aud => audience.Contains(aud.GetDisplayName()))
						.Aggregate((a, b) => a | b);
			}
			catch { }

			TokenInfo output = new TokenInfo
			{
				// Audience = audience,
				Authorization = token,
				AccountId = (string) aid,
				ScreenName = (string) sn,
				Discriminator = Convert.ToInt32(disc ?? -1),
				IsAdmin = (bool) (admin ?? false),
				IssuedAt = Convert.ToInt64(issuedAt),
				Email = (string) email,
				Expiration = Convert.ToInt64(expiration),
				Issuer = (string) issuer,
				IpAddress = (string) ip,
				GameKey = (string) gameKey,
				CountryCode = (string) countryCode,
				Requester = (string) requester,
				PermissionSet = (int)(perms ?? int.MaxValue)
			};
			if (output.Email != null)
				output.Email = Crypto.Decode(output.Email);
			
			// Begin validation; if the token fails any necessary criteria, throw auth exceptions.
			if (output.Expiration <= Timestamp.Now)
				throw new AuthException(output, "Token is expired.");

			// TODO: Remove this by 12/1.
			if (output.Audience.Contains(PlatformEnvironment.GameSecret))
			{
				Log.Warn(Owner.Will, "Token should be re-generated; it is using an old claims standard.", data: new
				{
					Token = token
				});
				return output;
			}
			
			if (output.GameKey != PlatformEnvironment.GameSecret)
				throw new AuthException(output, "Environment mismatch; token's game key is incorrect.");

			// All tokens are necessarily allowed to hit token-service.
			if (requestingService != Audience.TokenService.GetDisplayName())
				return output;
			
			// Ensure that the token is valid on the provided service.
			if (!output.Audience.Contains(Audience.All.GetDisplayName()) && !output.Audience.Contains(requestingService))
				throw new AuthException(output, "Audience mismatch; token is not permitted on this resource.");
			return output;
		}
		catch (AuthException)
		{
			throw;
		}
		catch (IntegrityException e)
		{
			throw new AuthException(encryptedToken: token, "Signature mismatch");
		}
		catch (Exception e)
		{
			throw new AuthException(encryptedToken: token, "Unable to decode token.");
		}
	}

	private static string GenerateToken(TokenInfo info)
	{
		Dictionary<string, object> claims = new Dictionary<string, object>();
		
		claims.Add(TokenInfo.DB_KEY_ACCOUNT_ID, info.AccountId);
		claims.Add(TokenInfo.DB_KEY_EXPIRATION, info.Expiration);
		claims.Add(TokenInfo.DB_KEY_ISSUER, ISSUER);
		claims.Add(TokenInfo.DB_KEY_ISSUED_AT, Timestamp.Now);
		claims.Add(TokenInfo.DB_KEY_PERMISSION_SET, info.PermissionSet);

		if (info.ScreenName != null)
			claims.Add(TokenInfo.DB_KEY_SCREENNAME, info.ScreenName);
		if (info.Discriminator > 0)
			claims.Add(TokenInfo.DB_KEY_DISCRIMINATOR, info.Discriminator);
		if (!string.IsNullOrWhiteSpace(info.Email))
			claims.Add(TokenInfo.DB_KEY_EMAIL_ADDRESS, Crypto.Encode(info.Email));
		if (!string.IsNullOrWhiteSpace(info.IpAddress))
			claims.Add(TokenInfo.DB_KEY_IP_ADDRESS, info.IpAddress);
		if (!string.IsNullOrWhiteSpace(info.CountryCode))
			claims.Add(TokenInfo.DB_KEY_COUNTRY_CODE, info.CountryCode);
		if (info.IsAdmin)
			claims.Add(TokenInfo.DB_KEY_IS_ADMIN, true);
		if (info.Requester != null)
			claims.Add(TokenInfo.DB_KEY_REQUESTER, info.Requester);
		if (info.GameKey != null)
			claims.Add(TokenInfo.DB_KEY_GAME, info.GameKey);

		return new JsonWebToken(claims).EncodedString;
	}

	/// <summary>
	/// Partially obscures a sensitive string for logging or otherwise visible purposes.  Values at the beginning and end remain visible.
	/// TODO: this might be useful in platform-common
	/// </summary>
	/// <param name="sensitive">The string to partially hide.</param>
	/// <param name="length">The number of characters at BOTH beginning and end to display.</param>
	/// <returns>A string in the format "foo...bar", where the ellipsis replaces everything in the middle of the string.</returns>
	private static string Obscure(string sensitive, int length = 4) => sensitive.Length > length * 2 ? $"{sensitive[..length]}...{sensitive[^length..]}" : "too_short";
}