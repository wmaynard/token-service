using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Jose;
using MongoDB.Bson.Serialization.Attributes;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using RCL.Logging;
using Rumble.Platform.Common.Exceptions;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Web;

namespace TokenService.Models;

public class JsonWebToken : PlatformDataModel
{
	private const JwsAlgorithm ALGORITHM = JwsAlgorithm.RS256;
	private static readonly string PUBLIC_KEY = PlatformEnvironment.Variable("PEM_PUBLIC");
	private static readonly string PRIVATE_KEY = PlatformEnvironment.Variable("PEM_PRIVATE");
	
	[BsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	public string EncodedString { get; init; }

	public JsonWebToken(Dictionary<string, object> claims)
	{
		if (PRIVATE_KEY == null || PUBLIC_KEY == null)
			throw new PlatformStartupException("Unable to complete request: RSA keys are missing.");
		
		RSAParameters rsaParams;
		using (StringReader rdr = new StringReader(PRIVATE_KEY))
		{
			PemReader pemReader = new PemReader(rdr);
			AsymmetricCipherKeyPair keyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject()
				?? throw new Exception("Could not read RSA private key");
			RsaPrivateCrtKeyParameters privateRsaParams = (RsaPrivateCrtKeyParameters)keyPair.Private;
			rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
		}
		using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
		{
			rsa.ImportParameters(rsaParams);
			EncodedString = JWT.Encode(claims, rsa, ALGORITHM);
		}
	}

	internal static Dictionary<string, object> Decode(string token)
	{
		if (PRIVATE_KEY == null || PUBLIC_KEY == null)
			throw new PlatformStartupException("Unable to complete request: RSA keys are missing.");
		
		RSAParameters rsaParams;

		using (StringReader rdr = new StringReader(PUBLIC_KEY))
		{
			PemReader pemReader = new PemReader(rdr);
			RsaKeyParameters publicKeyParams = (RsaKeyParameters)pemReader.ReadObject();
			if (publicKeyParams == null)
				throw new Exception("Could not read RSA public key");
			rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParams);
		}
		using (RSACryptoServiceProvider provider = new RSACryptoServiceProvider())
		{
			provider.ImportParameters(rsaParams);

			// This will throw if the signature is invalid
			string payload = JWT.Decode(token, provider, ALGORITHM);

			return JsonDocument.Parse(payload, JsonHelper.DocumentOptions)
				.RootElement
				.EnumerateObject()
				.ToDictionary(
					keySelector: json => json.Name, 
					elementSelector: json => AutocastJson(json.Value)
				);
		}
	}
	public static object AutocastJson(JsonElement element)
	{
		try
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.Array:
					return element.EnumerateArray().Select(AutocastJson).ToArray();
				case JsonValueKind.Object:
					return element
						.EnumerateObject()
						.ToDictionary(
							keySelector: json => json.Name, 
							elementSelector: json => AutocastJson(json.Value)
						);
				case JsonValueKind.False:
				case JsonValueKind.True:
					return element.GetBoolean();
				case JsonValueKind.Number:
					string test = element.ToString();
					try
					{
						return int.Parse(test);
					}
					catch (FormatException)
					{
						return double.Parse(test);
					}
					catch (OverflowException)
					{
						return long.Parse(test);
					}
					catch (Exception ex)
					{
						Log.Warn(Owner.Default, "Unable to convert JSON number value.", data: new {
							JSON = element
						}, exception: ex);
						return null;
					}
				case JsonValueKind.String:
					return element.GetString();
				case JsonValueKind.Undefined:
				case JsonValueKind.Null:
				default:
					return null;
			}
		}
		catch (Exception ex)
		{
			Log.Warn(Owner.Will, "Unable to convert JSON value.", data: new
			{
				JSON = element
			}, exception: ex);
			return null;
		}
	}
}