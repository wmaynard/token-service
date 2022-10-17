using System;
using System.Text.Json;
using Jose;
using MongoDB.Bson.Serialization;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;
using Rumble.Platform.Data;

namespace TokenService.Exceptions;

public class AuthException : PlatformException
{
	public const string GRAPHITE_KEY_ERRORS = "auth-errors";
	public string Reason { get; private set; }
	public TokenInfo TokenInfo { get; private set; }
	public string Origin { get; private set; }
	
	// TODO: These details do not show up in logs, requires fix in platform-common
	public AuthException(TokenInfo token, string reason) : base(reason ?? "Token is invalid.")
	{
		TokenInfo = token;
		Reason = reason;

		Graphite.Track(GRAPHITE_KEY_ERRORS, 1);
	}

	public AuthException(TokenInfo token, string origin, string reason) : this(token, reason) => Origin = origin;

	public AuthException(string encryptedToken, string reason) : this(token: null, reason)
	{
		// The token failed authorization, but we can still inspect the payload and claims it has.  This is very helpful for dumping to Loggly.
		// Attempt to extract the encrypted token's claims and cast them to the TokenInfo object regardless.
		try
		{
			RumbleJson data = JWT.Payload(encryptedToken);
			TokenInfo = data.ToModel<TokenInfo>(fromDbKeys: true);
			if (TokenInfo.Email != null)
				TokenInfo.Email = EncryptedString.Decode(TokenInfo.Email);
		}
		catch (Exception e)
		{
			Log.Warn(Owner.Will, "Unable to deserialize encrypted token.  Details will not be provided.", data: new
			{
				EncryptedToken = encryptedToken
			}, exception: e);
		}
	}
}