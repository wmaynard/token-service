# token-service

An API for authenticating users

# Introduction

The Token Service is the entry point for all Platform services that require authentication.  Consumers of this service can request a JSON Web Token (JWT, or simply "token" in this document), which contains information embedded in it that can be used to uniquely identify a user.  Every token is signed and validated by Token Service with a secret key, which guarantees we know the token can be trusted.

Administrator tokens can also be issued to internal consumers.  Previously, the Groovy services required passing a secret value around to authenticate admins.  With a custom token for each consumer, we can audit who's using services in addition to relying on secrets less.

Token payloads are not encrypted.  Any value contained within the token can be read by anyone, so personally identifiable information (PII) should never be embedded into them without first being encrypted.  To verify that your data is appropriately hidden, you can decode the information easily on the following website: https://jwt.io/

It's important to note that when you generate a new token, previously-issued tokens are not necessarily invalid.  Tokens are invalid when:

1. Their expiration date has passed.
2. Sufficient new tokens have been generated for an account.  (See `/Models/Identity.MAX_AUTHORIZATIONS_KEPT`).
3. An administrator has invalidated an account's tokens.

# Glossary

| Term | Definition |
| ---: | :--- |
| AccountID (aid) | With regards to player accounts, this refers to their MongoDB-generated key.  For administrators, this is a user-defined unique identifier. |
| Authorization | An HTTP header in web requests in the form of `Bearer {token}`, where "token" is a generated JWT from Token Service. |
| Ban | Completely bans the **AccountId** in question.  Regardless of the authorization granted to them, a banned account will fail all validation. |
| Game Key | Each game has a unique key that's used to identify it.  When a token is generated, it is linked to a specific resource by a key like this. |
| Invalidate | Marks every issued token as invalid.  When a consumer tries to access an API with an invalid token, they will receive an error.  They will need to generate a new token to continue using our services. |
| Request | An HTTP request.  |
| Rumble Key / Secret | A secret string value.  When this value matches what the service expects, the service knows it can trust the request's source.  This secret is unique to each environment. | 
| Signature | The ending component of a token, validated by RSA encryption.  This is used to verify Token authorship. |
| Token / JWT | An encrypted token containing information about what resources a user can access and anything Platform uses to uniquely identify a user.  Tokens are sent in web requests as an HTTP header, in the format `"Bearer {token}"`. |
| Trusted Client | A Rumble product or employee's device.  This typically means they have a whitelisted IP address, access to the rumble key, or both. |

# Consuming The Service

Every secured endpoint requires an `Authorization` header with a value of `Bearer {token}`, where the token is issued by this service via `/token/generate`.  This token must be valid for that particular request (e.g. basic or admin privileges).

`/secured/generateToken` requires the `RUMBLE_KEY`, a secret internal value but requires no other authentication.  Any request that does not contain this value will fail.  The `/secured` root is only accessible from whitelisted IP addresses. 

# Endpoints

## Example Flow

1. A trusted client sends a request to `/secured/generateToken`.  Token Service returns a response containing an `authorization` and `tokenInfo`.
2. The client stores `authorization.token` and uses this value in its header as its `Authorization` header with all other Rumble services.
3. For those future requests, all Platform services send a request to `/token/validate`.  If the token is valid, token details are returned in the response body in `tokenInfo`.

## Top Level

| Method  | Endpoint  | Description | Required Parameters | Optional Parameters |
| ---:    | :---      | :---        | :---                | :---                |
| GET | `/token/health`| Health check, required by the load balancer. | | |

## Admin

All `/admin` endpoints require a valid admin token.

| Method  | Endpoint  | Description | Required Parameters | Optional Parameters |
| ---:    | :---      | :---        | :---                | :---                |
| PATCH | `/token/admin/ban` | Bans an account from all services. | `aid` | |
| PATCH | `/token/admin/invalidate` | Invalidates all existing tokens for an account. | `aid` | |
| PATCH | `/token/admin/unban` | Removes a ban from an account. | `aid` | |

## TopController
| Method  | Endpoint  | Description | Required Parameters | Optional Parameters |
| ---:    | :---      | :---        | :---                | :---                |
| GET | `/token/validate/` | Checks to see if a token is valid.  Returns `tokenInfo` if valid, otherwise an error. | | |

## SecuredController

All `/secured` endpoints are protected by whitelisted IP addresses when deployed.  This whitelist can be found in the `/.gitlab/dev.values.yaml` file, under the application whitelist section.

| Method  | Endpoint  | Description | Required Parameters | Optional Parameters |
| ---:    | :---      | :---        | :---                | :---                |
| POST | `/secured/token/generate` | Creates a token with account information embedded in it. | `aid`, `origin` | `days`, `discriminator`, `email`, `key`, `screenname`

### Parameter breakdown for `/generateToken`:
* `aid`: The Mongo-issued AccountId for players, or a unique identifier for the Rumble product / user requesting it.
* `days`: The number of days the token should be valid for.  The maximum value for this variable for non-admin tokens is 5 and for admin tokens is 3650 (ten years); larger values will be ignored.
* `email`: The email address for an account.  This value is stored in Mongo along with the account.  It is embedded in the token as an encrypted value.
* `key`: The Rumble Key.  Any token created with this will be an Administrator.
* `origin`: Where the request is coming from (e.g. Tower Game Server, Publishing App, etc).
* `screenname`: The screenname for an account.

## Project Requirements

The following **environment variables** must exist on the server where the service is running:

	{
	  "GRAPHITE": "graphite.rumblegames.com:2003",
	  "LOGGLY_URL": "https://logs-01.loggly.com/bulk/{id}/tag/token-service",
	  "MONGODB_NAME": "token-service-107",
	  "MONGODB_URI": "mongodb+srv://{connection string},
	  "RUMBLE_COMPONENT": "token-service",
	  "RUMBLE_DEPLOYMENT": "{deployment id}",
	  "RUMBLE_KEY": "{secret}",
	  "GAME_GUKEY": "{game key}"
	}

## Public / Private Key Files (.pem)

Token Service relies on the use of two key files to authenticate tokens via RSA.  These files are `public.pem` and `private.pem`, respectively.  These files should never be committed into a repository and must be kept closely guarded.  Their values are stored in GitLab's CI/CD `File` variables.  Each environment has its own key files.

To generate new key files, run the following commands in Terminal:

1. `openssl genrsa -in private.pem 512`
2. `openssl rsa -in private.pem -pubout -out public.pem`

When working locally, you can drop these into the base directory of your project.

## Maintenance

Because of its very nature, this project can not leverage the `PlatformAuthorizationFilter`.

Consequently, the standard way of accessing tokens via `PlatformController.Token` doesn't work right out of the box.  `/Controllers/TokenAuthController.cs` restores this functionality.

## Future Updates, Optimizations, and Nice-to-Haves

* A list of aliases for all accounts would be nice for auditing purposes.
* Explore additional options for adding permissions (audiences) for tokens.

## Troubleshooting

### _I'm unable to generate a token anywhere other than my local machine._

It's important that our token generation remains as secure as possible.  While it would be a huge problem if the `Rumble Key` was ever leaked, we have an extra layer of security for tokens: you must be whitelisted in order to generate tokens.  Refer to the **SecuredController** section for more information.

### _How do I find the `Rumble Key`?_

It's a bad idea to document exactly how to find it, so contact Platform instead.

### _I'm trying to generate an admin token and am sending a `Rumble Key`, but my token says it isn't an administrator when I validate it._

You probably have a mismatched Rumble Key.  By design, Token Service doesn't tell you when you have incorrect admin credentials, since this could be used by malicious actors to identify what the problem is.

### _I generated a token that should still be valid, but any time I try to use it I get an error._

Tokens can fall off from accounts if too many have been generated.  Generate a new token and make sure you're updating your token whenever a new one is issued.