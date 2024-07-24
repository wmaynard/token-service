# token-service

An API for authenticating users

## Acknowledgment

Token Service was originally created for Rumble Entertainment (which later became R Studios), a mobile gaming company.  The service was responsible for generating and validating all of our tokens for use with our API calls.  Tokens could be either Player or Admin tokens with dynamic permissions assigned to them.

R Studios unfortunately closed its doors in July 2024.  This project has been released as open source with permission.

As of this writing, there may still be existing references to Rumble's resources, such as Confluence links, but their absence doesn't have any significant impact.  Some documentation will also be missing until it can be recreated here, since with the company closure any feature specs and explainer articles originally written for Confluence / Slack channels were lost.

While Rumble is shutting down, I'm grateful for the opportunities and human connections I had working there.

One Confluence article was saved as PDF explaining how our player accounts work and how our player data was organized.  You can read it [here](Player%20Accounts%20Overview.pdf).  I wrote this article as a detailed explainer piece for our legal and compliance team.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.

# Introduction

The Token Service is the entry point for all Platform services that require authentication.  Consumers of this service can request a JSON Web Token (JWT, or simply "token" in this document), which contains information embedded in it that can be used to uniquely identify a user.  Every token is signed and validated by Token Service with a secret key, which guarantees we know the token can be trusted.

Administrator tokens can also be issued to internal consumers.  Previously, the Groovy services required passing a secret value around to authenticate admins.  With a custom token for each consumer, we can audit who's using services in addition to relying on secrets less.

Token payloads are not encrypted.  Any value contained within the token can be read by anyone, so personally identifiable information (PII) should never be embedded into them without first being encrypted.  To verify that your data is appropriately hidden, you can decode the information easily on the following website: https://jwt.io/

It's important to note that when you generate a new token, previously-issued tokens are not necessarily invalid.  Tokens are invalid when:

1. Their expiration date has passed.
2. Sufficient new tokens have been generated for an account.  (See `/Models/Identity.MAX_AUTHORIZATIONS_KEPT`).
3. An administrator has invalidated an account's tokens.

# Glossary

|                Term | Definition                                                                                                                                                                                                                     |
|--------------------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
|     AccountID (aid) | With regards to player accounts, this refers to their MongoDB-generated key.  For administrators, this is a user-defined unique identifier.                                                                                    |
|            Audience | An array of unique resource IDs that indicate a token is allowed to access that particular resource.                                                                                                                           |
|       Authorization | An HTTP header in web requests in the form of `Bearer {token}`, where "token" is a generated JWT from Token Service.                                                                                                           |
|                 Ban | See BANS.md for specific documentation.                                                                                                                                                                                        |
|            Game Key | Each game has a unique key that's used to identify it.  When a token is generated, it is linked to a specific resource by a key like this.                                                                                     |
|          Invalidate | Marks every issued token as invalid.  When a consumer tries to access an API with an invalid token, they will receive an error.  They will need to generate a new token to continue using our services.                        |
|             Request | An HTTP request.                                                                                                                                                                                                               |
| Rumble Key / Secret | A secret string value.  When this value matches what the service expects, the service knows it can trust the request's source.  This secret is unique to each environment.                                                     | 
|           Signature | The ending component of a token, validated by RSA encryption.  This is used to verify Token authorship.                                                                                                                        |
|         Token / JWT | An encrypted token containing information about what resources a user can access and anything Platform uses to uniquely identify a user.  Tokens are sent in web requests as an HTTP header, in the format `"Bearer {token}"`. |
|      Trusted Client | A Rumble product or employee's device.  This typically means they have a whitelisted IP address, access to the rumble key, or both.                                                                                            |

# Consuming The Service

Every secured endpoint requires an `Authorization` header with a value of `Bearer {token}`, where the token is issued by this service via `/token/generate`.  This token must be valid for that particular request (e.g. basic or admin privileges).

`/secured/generateToken` requires the `RUMBLE_KEY`, a secret internal value but requires no other authentication.  Any request that does not contain this value will fail.  The `/secured` root is only accessible from whitelisted IP addresses. 

# Endpoints

## Example Flow

1. A trusted client sends a request to `/secured/generateToken`.  Token Service returns a response containing an `authorization` and `tokenInfo`.
2. The client stores `authorization.token` and uses this value in its header as its `Authorization` header with all other Rumble services.
3. For those future requests, all Platform services send a request to `/token/validate`.  If the token is valid, token details are returned in the response body in `tokenInfo`.

## Top Level

| Method | Endpoint        | Description                                  | Required | Optional | Internal Consumers | External Consumers |
|-------:|:----------------|:---------------------------------------------|:---------|:---------|:-------------------|:-------------------|
|    GET | `/token/health` | Health check, required by the load balancer. |          |          | Load balancer      |                    |

## Admin

All `/admin` endpoints require a valid admin token.

| Method | Endpoint                  | Description                                     | Required    | Optional     | Internal Consumers | External Consumers |
|-------:|:--------------------------|:------------------------------------------------|:------------|:-------------|:-------------------|:-------------------|
|   POST | `/token/admin/ban`        | Bans an account from specific services.         | `accountId` | `expiration` | Portal             |                    |
|  PATCH | `/token/admin/invalidate` | Invalidates all existing tokens for an account. | `accountId` |              | Portal             |                    | 
|  PATCH | `/token/admin/unban`      | Removes a ban from an account.                  | `accountId` |              | Portal             |                    |

## TopController

| Method | Endpoint           | Description                                                                           | Required | Optional   | Internal Consumers         | External Consumers |
|-------:|:-------------------|:--------------------------------------------------------------------------------------|:---------|:-----------|:---------------------------|:-------------------|
|    GET | `/token/validate/` | Checks to see if a token is valid.  Returns `tokenInfo` if valid, otherwise an error. | `origin` | `endpoint` | All Services <br /> Portal |                    |

### Paramater breakdown for `/validate`:
* `origin`: This is the service / project name, as determined by `PlatformEnvironment.ServiceName`.  This is checked against the audience of a token to determine its validity.
* `endpoint`: This is the endpoint, if applicable, that's asking for the validation.

Both of these fields are used to add important detail to logs when auth exceptions are encountered.  For example, if we were under attack, we would see which endpoints were being used and which service is exposed.

## SecuredController

All `/secured` endpoints are protected by whitelisted IP addresses when deployed.  This whitelist can be found in the `/.gitlab/dev.values.yaml` file, under the application whitelist section.

| Method | Endpoint                  | Description                                              | Required                  | Optional                                                                              | Internal Consumers              | External Consumers |
|-------:|:--------------------------|:---------------------------------------------------------|:--------------------------|:--------------------------------------------------------------------------------------|:--------------------------------|:-------------------|
|   POST | `/secured/token/generate` | Creates a token with account information embedded in it. | `accountId`<br />`origin` | `audience`<br />`days`<br />`discriminator`<br />`email`<br />`key`<br />`screenname` | player-service<br />dmz-service |                    |

### Parameter breakdown for `/generateToken`:
* `accountId`: The Mongo-issued AccountId for players, or a unique identifier for the Rumble product / user requesting it.
* `audience`: This is an array of resource IDs indicating where a token is valid.  Within platform, this is maintained in platform-common/enums/Project.cs.  A wildcard may be used to grant access to all resources.
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

1. `openssl genrsa -out private.pem 512`
2. `openssl rsa -in private.pem -pubout -out public.pem`

When working locally, you can drop these into the base directory of your project.

## Maintenance

Because of its very nature, this project can not leverage the `PlatformAuthorizationFilter`.

Consequently, the standard way of accessing tokens via `PlatformController.Token` doesn't work right out of the box.  `/Controllers/TokenAuthController.cs` restores this functionality.

### Regarding Audiences

Audience strings are maintained in `platform-common/Enums/Audience.cs`.  These enum values use a `Display` attribute that is responsible for giving us the respective audience string.  In order to leverage the full security of a token, these values must be kept up-to-date.  Whenever a new project or service is created, this enum should also be updated.

Whenever any new project begins, to be compatible with Rumble token security:
     1. Add an entry to this enum.
     2. Bump the common version numbers and push changes.
     3. Update token-service's common package and push changes.

 If the new project is something that players should be able to access, you will also need to update a constant in `player-service/Controllers/TopController/TOKEN_AUDIENCE`.

Any relevant admin tokens will also need to be regenerated - and this will have to be done across all environments.

While this can be a nuisance, it's important to do this maintenance.  The audience acts as our permissions system and dictates who can interact with what.  For example, it doesn't make sense for player tokens to directly interact with internal tools like dynamic config or the calendar service.

## Future Updates, Optimizations, and Nice-to-Haves

* A list of aliases for all accounts would be nice for auditing purposes.

## Troubleshooting

### _I'm unable to generate a token anywhere other than my local machine._

It's important that our token generation remains as secure as possible.  While it would be a huge problem if the `Rumble Key` was ever leaked, we have an extra layer of security for tokens: you must be whitelisted in order to generate tokens.  Refer to the **SecuredController** section for more information.

### _How do I find the `Rumble Key`?_

Contact someone in platform, or ask the Slack channel (#platform).

### _I'm trying to generate an admin token and am sending a `Rumble Key`, but my token says it isn't an administrator when I validate it._

You probably have a mismatched Rumble Key.  By design, Token Service doesn't tell you when you have incorrect admin credentials, since this could be used by malicious actors to identify what the problem is.

### _I generated a token that should still be valid, but any time I try to use it I get an error._

Tokens can fall off from accounts if too many have been generated.  Generate a new token and make sure you're updating your token whenever a new one is issued.