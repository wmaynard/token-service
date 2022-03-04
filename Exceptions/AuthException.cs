using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Interop;
using Rumble.Platform.Common.Web;

namespace TokenService.Exceptions
{
	public class AuthException : PlatformException
	{
		public const string GRAPHITE_KEY_ERRORS = "auth-errors";
		public string Reason { get; private set; }
		public TokenInfo Token { get; private set; }

		public AuthException(TokenInfo token, string reason) : base(reason ?? "Token is invalid.")
		{
			Token = token;
			Reason = reason;
			
			Graphite.Track(GRAPHITE_KEY_ERRORS, 1);
		}
	}
}