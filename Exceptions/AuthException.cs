using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;

namespace TokenService.Exceptions
{
	public class AuthException : PlatformException
	{
		public const string GRAPHITE_KEY_ERRORS = "auth-errors";
		
		public TokenInfo Token { get; private set; }
		public string Reason { get; private set; }

		public AuthException(TokenInfo token, string reason) : base("Token is invalid.")
		{
			Token = token;
			Reason = reason;
			
			Graphite.Track(GRAPHITE_KEY_ERRORS, 1);
		}
	}
}