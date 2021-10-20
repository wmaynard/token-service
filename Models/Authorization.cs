using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
using TokenService.Exceptions;

namespace TokenService.Models
{
	public class Authorization : PlatformDataModel
	{
		internal const string ISSUER = "Rumble Token Service";
		private static readonly string SIGNATURE = PlatformEnvironment.Variable("TOKEN_SECRET");
		internal static readonly string ADMIN_SECRET = PlatformEnvironment.Variable("RUMBLE_KEY");
		internal static readonly string AUDIENCE = PlatformEnvironment.Variable("GAME_GUKEY");

		private const string CLAIM_KEY_ACCOUNT_ID = "aid";
		private const string CLAIM_KEY_SCREENNAME = "sn";
		private const string CLAIM_KEY_DISCRIMINATOR = "d";
		private const string CLAIM_KEY_IS_ADMIN = "su";
		
		internal const string DB_KEY_ISSUER = "iss";
		internal const string DB_KEY_ORIGIN = "org";
		internal const string DB_KEY_EXPIRATION = "xp";
		internal const string DB_KEY_TOKEN = "val";
		internal const string DB_KEY_IS_ADMIN = "su";
		internal const string DB_KEY_IS_VALID = "ok";
		internal const string DB_KEY_CREATED = "ts";

		public const string FRIENDLY_KEY_ISSUER = "issuer";
		public const string FRIENDLY_KEY_ORIGIN = "origin";
		public const string FRIENDLY_KEY_TOKEN = "token";
		public const string FRIENDLY_KEY_IS_ADMIN = "isAdmin";
		public const string FRIENDLY_KEY_IS_VALID = "isValid";
		public const string FRIENDLY_KEY_EXPIRATION = "expiration";
		public const string FRIENDLY_KEY_CREATED = "created";
		public const string FRIENDLY_KEY_REMAINING = "secondsRemaining";
		
		[BsonElement(DB_KEY_ISSUER)]
		[JsonProperty(FRIENDLY_KEY_ISSUER, NullValueHandling = NullValueHandling.Include)]
		public string Issuer { get; private set; }
		[BsonElement(DB_KEY_ORIGIN)]
		[JsonProperty(FRIENDLY_KEY_ORIGIN, NullValueHandling = NullValueHandling.Include)]
		public string Origin { get; internal set; }
		[BsonElement(DB_KEY_TOKEN)]
		[JsonProperty(FRIENDLY_KEY_TOKEN, NullValueHandling = NullValueHandling.Include)]
		public string EncryptedToken { get; internal set; }
		
		[BsonElement(DB_KEY_IS_ADMIN), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_IS_ADMIN, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool IsAdmin { get; private set; }
		[BsonElement(DB_KEY_IS_VALID)]
		[JsonProperty(FRIENDLY_KEY_IS_VALID, DefaultValueHandling = DefaultValueHandling.Include)]
		public bool IsValid { get; internal set; }
		[BsonElement(DB_KEY_EXPIRATION)]
		[JsonProperty(FRIENDLY_KEY_EXPIRATION, DefaultValueHandling = DefaultValueHandling.Include)]
		public long Expiration { get; internal set; }
		[BsonElement(DB_KEY_CREATED), BsonIgnoreIfDefault]
		[JsonProperty(FRIENDLY_KEY_CREATED, DefaultValueHandling = DefaultValueHandling.Ignore)]
		public long Created { get; internal set; }
		
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
			
			EncryptedToken = GenerateToken(info, IsAdmin);
		}

		internal static TokenInfo Decode(string token)
		{
			token = token.Replace("Bearer ", "");
			try
			{
				TokenValidationParameters tvp = new TokenValidationParameters()
				{
					ValidateActor = false,
					ValidateAudience = true,
					ValidAudience = AUDIENCE,
					ValidateIssuer = true,
					ValidIssuer = ISSUER,
					ValidateIssuerSigningKey = true,
					RequireSignedTokens = true,
					IssuerSigningKeys = new[] {new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SIGNATURE))},
					ValidateLifetime = true,
					RequireExpirationTime = true,
					ClockSkew = TimeSpan.FromHours(12) // TODO: not sure what this does and can probably remove it or lower it, but it was in the example I found
				};
			
				ClaimsPrincipal cp = new JwtSecurityTokenHandler().ValidateToken(token, tvp, out SecurityToken validatedToken);
				
				string aid = cp.FindFirstValue(claimType: TokenInfo.DB_KEY_ACCOUNT_ID);
				string sn = cp.FindFirstValue(claimType: TokenInfo.DB_KEY_SCREENNAME);
				int disc = int.Parse(cp.FindFirstValue(claimType: TokenInfo.DB_KEY_DISCRIMINATOR) ?? "0");
				bool admin = cp.FindFirstValue(claimType: TokenInfo.DB_KEY_IS_ADMIN) == true.ToString();
				string issuer = cp.FindFirstValue(claimType: TokenInfo.DB_KEY_ISSUER);
				string ip = cp.FindFirstValue(claimType: TokenInfo.DB_KEY_IP_ADDRESS);
				long exp = long.Parse(cp.FindFirstValue(claimType: TokenInfo.DB_KEY_EXPIRATION));

				TokenInfo output = new TokenInfo(token)
				{
					AccountId = aid,
					ScreenName = sn,
					Discriminator = disc,
					IsAdmin = admin,
					Expiration = exp,
					Issuer = issuer,
					IpAddress = ip
				};

				return output;
			}
			catch (SecurityTokenInvalidSignatureException e)
			{
				Log.Critical(Owner.Default, "Token signature mismatch!  Someone may be trying to penetrate our security.", data: new
				{
					EncryptedToken = token
				}, exception: e);
				Graphite.Track(AuthException.GRAPHITE_KEY_ERRORS, 1_000); // exaggerate this to raise alarms
				throw;
			}

		}

		private string GenerateToken(TokenInfo info, bool isAdmin = false)
		{
			string secret = PlatformEnvironment.Variable("TOKEN_SECRET") ?? throw new ArgumentNullException(); // TODO: Custom Exception, required value flag?
			
			SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
			SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

			List<Claim> claims = new List<Claim>()
			{
				new Claim(type: TokenInfo.DB_KEY_ACCOUNT_ID, value: info.AccountId)
			};
			if (info.ScreenName != null)
				claims.Add(new Claim(type: TokenInfo.DB_KEY_SCREENNAME, value: info.ScreenName));
			if (info.Discriminator > 0)
				claims.Add(new Claim(type: TokenInfo.DB_KEY_DISCRIMINATOR, value: info.Discriminator.ToString()));
			// if (!string.IsNullOrWhiteSpace(info.Email))
			// 	claims.Add(new Claim(type: UserInfo.DB_KEY_EMAIL, value: info.Email));
			if (!string.IsNullOrWhiteSpace(info.IpAddress))
				claims.Add(new Claim(type: TokenInfo.DB_KEY_IP_ADDRESS, info.IpAddress));
			if (isAdmin)
				claims.Add(new Claim(type: CLAIM_KEY_IS_ADMIN, value: true.ToString()));

			SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
			{
				Audience = AUDIENCE,
				Issuer = ISSUER,
				IssuedAt = DateTime.UtcNow,
				NotBefore = DateTime.UtcNow,
				Expires = DateTime.UnixEpoch.AddSeconds(Expiration),
				Subject = new ClaimsIdentity(claims),
				SigningCredentials = credentials
			};
			
			
			Graphite.Track("tokens-generated", 1);
			if (isAdmin)
			{
				Log.Info(Owner.Default, "Admin token generated.");
				Graphite.Track("admin-tokens-generated", 1);
			}
			
			return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
		}

		// Other fields that player service was tracking:
		// remoteAddress, geoipAddress, country, serverTime, accountId, requestId?, token
	}
}