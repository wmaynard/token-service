using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Rumble.Platform.Common.Web;
using Rumble.Platform.CSharp.Common.Interop;
using TokenService.Exceptions;
using TokenService.Models;
using TokenService.Services;

namespace TokenService.Controllers
{
	[ApiController, Route("token")]
	public class TopController : TokenAuthController
	{
		public TopController(IdentityService identityService, IConfiguration config) : base(identityService, config) { }
		
		[HttpGet, Route("validate")]
		public ActionResult Validate() // TODO: common | EncryptedToken
		{
			if (Token.IsExpired)
				throw new AuthException(Token, "Token has expired.");

			Identity id = _identityService.Find(Token.AccountId);
			if (id.Banned)
				throw new AuthException(Token, "Account was banned.");
			
			Authorization authorization = id.Authorizations.FirstOrDefault(auth => auth.Expiration == Token.Expiration && auth.EncryptedToken == Token.Authorization);

			if (authorization == null)
				throw new AuthException(Token, "Token is too old and has been replaced by newer tokens.");
			if (!authorization.IsValid)
				throw new AuthException(Token, "Token was invalidated.");

			string name = Token.IsAdmin
				? "admin-tokens-validated"
				: "tokens-validated";
			Graphite.Track(name, 1);
			
			return Ok(Token.ResponseObject);
		}
		
		// /health is in the base, TokenAuthController
	}
}