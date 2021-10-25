using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Jose;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
using TokenService.Exceptions;

namespace TokenService.Models
{
	public class Authorization : PlatformDataModel
	{
		internal const string ISSUER = "Rumble Token Service";
		internal static readonly string ADMIN_SECRET = PlatformEnvironment.Variable("RUMBLE_KEY");
		internal static readonly string AUDIENCE = PlatformEnvironment.Variable("GAME_KEY");

		private const string CLAIM_KEY_ISSUED_AT = "iat";
		private const string CLAIM_KEY_AUDIENCE = "aud";
		
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
		[JsonProperty(FRIENDLY_KEY_CREATED, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long Created { get; internal set; }
		
		[BsonElement(DB_KEY_TOKEN)]
		[JsonProperty(FRIENDLY_KEY_TOKEN, NullValueHandling = NullValueHandling.Include)]
		public string EncryptedToken { get; internal set; }
		[BsonElement(DB_KEY_EXPIRATION)]
		[JsonProperty(FRIENDLY_KEY_EXPIRATION, DefaultValueHandling = DefaultValueHandling.Include)]
		public long Expiration { get; internal set; }
		
		[BsonElement(DB_KEY_IS_ADMIN), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_IS_ADMIN, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsAdmin { get; private set; }
		
		[BsonElement(DB_KEY_IS_VALID)]
		[JsonProperty(FRIENDLY_KEY_IS_VALID, DefaultValueHandling = DefaultValueHandling.Include)]
		public bool IsValid { get; internal set; }
		
		[BsonElement(DB_KEY_ISSUER)]
		[JsonProperty(FRIENDLY_KEY_ISSUER, NullValueHandling = NullValueHandling.Include)]
		public string Issuer { get; private set; }
		
		[BsonElement(DB_KEY_ORIGIN)]
		[JsonProperty(FRIENDLY_KEY_ORIGIN, NullValueHandling = NullValueHandling.Include)]
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

			lifetimeDays = IsAdmin
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

			info.Expiration = Expiration;
			info.IsAdmin = isAdmin;
			
			EncryptedToken = GenerateToken(info);
		}

		/// <summary>
		/// Decodes and validates an encrypted token.
		/// </summary>
		/// <param name="token">The token to decode.</param>
		/// <returns>Information that was embedded in the token.</returns>
		internal static TokenInfo Decode(string token)
		{
			token = token.Replace("Bearer ", "");

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
				claims.TryGetValue(CLAIM_KEY_AUDIENCE, out object audiences);
				claims.TryGetValue(CLAIM_KEY_ISSUED_AT, out object issuedAt);

				TokenInfo output = new TokenInfo(token)
				{
					AccountId = (string) aid,
					ScreenName = (string) sn,
					Discriminator = Convert.ToInt32(disc ?? -1),
					IsAdmin = (bool) (admin ?? false),
					Email = (string) email,
					Expiration = Convert.ToInt64(expiration),
					Issuer = (string) issuer,
					IpAddress = (string) ip
				};
				if (output.Email != null)
					output.Email = Crypto.Decode(output.Email);

				if (!(audiences as object[]).Contains(AUDIENCE))
					throw new AuthException(output, "Audience mismatch.");
				if (output.Expiration <= UnixTime)
					throw new AuthException(output, "Token is expired.");
				return output;
			}
			catch (AuthException)
			{
				throw;
			}
			catch (IntegrityException e)
			{
				Log.Critical(Owner.Default, "Token signature mismatch!  Someone may be trying to penetrate our security.", data: new
				{
					EncryptedToken = token
				}, exception: e);
				Graphite.Track(AuthException.GRAPHITE_KEY_ERRORS, 1_000); // exaggerate this to raise alarms
				throw new AuthException(null, "Signature mismatch");
			}
			catch (Exception e)
			{
				Log.Error(Owner.Default, "Unable to decode token.", data: new
				{
					EncodedToken = token
				}, exception: e);
				throw;
			}
		}

		private static string GenerateToken(TokenInfo info)
		{
			Dictionary<string, object> claims = new Dictionary<string, object>();
			
			claims.Add(TokenInfo.DB_KEY_ACCOUNT_ID, info.AccountId);
			claims.Add(TokenInfo.DB_KEY_EXPIRATION, info.Expiration);
			claims.Add(TokenInfo.DB_KEY_ISSUER, ISSUER);
			claims.Add(CLAIM_KEY_ISSUED_AT, UnixTime);
			claims.Add(CLAIM_KEY_AUDIENCE, new string[]{ AUDIENCE });

			if (info.ScreenName != null)
				claims.Add(TokenInfo.DB_KEY_SCREENNAME, info.ScreenName);
			if (info.Discriminator > 0)
				claims.Add(TokenInfo.DB_KEY_DISCRIMINATOR, info.Discriminator);
			if (!string.IsNullOrWhiteSpace(info.Email))
				claims.Add(TokenInfo.DB_KEY_EMAIL_ADDRESS, Crypto.Encode(info.Email));
			if (!string.IsNullOrWhiteSpace(info.IpAddress))
				claims.Add(TokenInfo.DB_KEY_IP_ADDRESS, info.IpAddress);
			if (info.IsAdmin)
				claims.Add(TokenInfo.DB_KEY_IS_ADMIN, true);

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
		
		// Other fields that player service was tracking:
		// remoteAddress, geoipAddress, country, serverTime, accountId, requestId?, token
	}
}