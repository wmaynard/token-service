using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Web;

namespace TokenService.Exceptions
{
	public class AuthException : PlatformException
	{
		public TokenInfo Token { get; private set; }
		public string Reason { get; private set; }

		public AuthException(TokenInfo token, string reason) : base("Token is invalid.")
		{
			Token = token;
			Reason = reason;
		}
	}
}