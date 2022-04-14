using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Web;

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

	public AuthException(TokenInfo token, string origin, string reason) : this(token, reason)
	{
		Origin = origin;
	}
}