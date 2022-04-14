using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Interop;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers;

[ApiController, Route("token")]
public class TopController : TokenAuthController
{
	public TopController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
	
	[HttpGet, Route("validate")]
	public ActionResult Validate() // TODO: common | EncryptedToken
	{
		string origin = Optional<string>("origin");
		// TODO: Add origin to TokenInfo
		// TODO: Create JIRA task to make origin REQUIRED
		
		if (Token.IsExpired)
			throw new AuthException(Token, origin, "Token has expired.");

		Identity id = _identityService.Find(Token.AccountId);
		if (id.Banned)
			throw new AuthException(Token, origin, "Account was banned.");
		
		Authorization authorization = id.Authorizations.FirstOrDefault(auth => auth.Expiration == Token.Expiration && auth.EncryptedToken == Token.Authorization);

		if (authorization == null)
			throw new AuthException(Token, origin, "Token is too old and has been replaced by newer tokens.");
		if (!authorization.IsValid)
			throw new AuthException(Token, origin, "Token was invalidated.");

		string name = Token.IsAdmin
			? "admin-tokens-validated"
			: "tokens-validated";
		Graphite.Track(name, 1);
		
		return Ok(Token.ResponseObject);
	}
	
	// /health is in the base, TokenAuthController
}