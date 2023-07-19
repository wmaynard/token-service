# Banning Players

In an ideal world, all of our players are on good behavior and use our systems the way we want them to be used.  Unfortunately, this is not in human nature, and we occasionally need to police our user base.  For offending players, we need to block them out of our systems to limit the damage they can cause.

In the past, this has been done in the form of service-specific bans.  Chat-service could ban people from itself, and token-service would prevent tokens from being generated at all.

This was a rudimentary system that worked well for what we needed at the time.  However, as our systems scale and we add more servers - and more features players can abuse - we need a better solution.  Rather than continue to implement multiple ban systems, token-service offers a unified experience.

## Glossary

|        Term | Definition                                                                                                                                            |
|------------:|:------------------------------------------------------------------------------------------------------------------------------------------------------|
|    Audience | The enum used in platform-common for server or feature identification.                                                                                |
|  Permission | Effectively a synonym for an Audience, a Permission is the integer representation of an Audience.                                                     |
|         Ban | A restriction placed on an account that blocks access to specific features.  Does not prevent token generation, but does limit token utility.         |
|  Expiration | A Unix timestamp indicating when a ban should no longer be valid.                                                                                     |
|     Feature | A specific function or subset of functions within a Service, e.g. SecondaryMarket.                                                                    |
|      Filter | A piece of code that runs before or after endpoint work.  Within the context of this project we're primarily concerned with the Authorization Filter. |
|     Service | An entire .NET project.                                                                                                                               |
| Token / JWT | An encoded string containing a JSON payload that's signed and authenticated by this service.                                                          |
|   TokenInfo | The platform-common class used to hold decoded JWT data.                                                                                              |

## Token Payload

Before we dive further into bans, it's helpful to go over what a token actually looks like when decoded.  Below is a sample token validation for a regular player token:
    
```
"token": {
    "aid": "649cd8db2f660825220eea45",
    "accountId": "649cd8db2f660825220eea45",
    "audience": [
        "chat-service",
        "dmz-service",
        "leaderboard-service",
        "mail-service",
        "matchmaking-service",
        "multiplayer-service",
        "nft-service",
        "player-service",
        "pvp-service",
        "receipt-service",
        "tower-service"
    ],
    "permissions": 159590,
    "discriminator": 7170,
    "expiration": 1690147795,
    "ip": "71.198.50.59",
    "country": "US",
    "issuedAt": 1689715795,
    "issuer": "Rumble Token Service",
    "game": "57901c6df82a45708018ba73b8d16004",
    "origin": "player-service",
    "screenname": "Player4b94c30",
    "username": "Player4b94c30#7170",
    "bans": [
        {
            "permissions": 1,
            "expiration": null,
            "reason": "He's a cheat",
            "id": "014f9b17-aef9-4518-89cc-449b41c6953d",
            "audience": [
                "calendar-service"
            ]
        }
    ]
}
```

For fields relevant to banning, we're looking at:

|     Field | Relevance                                                                                                        |
|----------:|:-----------------------------------------------------------------------------------------------------------------|
| accountId | The player's account ID, issued from player-service.                                                             |
|  audience | A verbose listing of services and features that the token can access.  **This is not necessarily exhaustive.\*** |
|      bans | An array of bans levied against the player.                                                                      |

As an important note, the `audience` fields here are solely for readability purposes; they aren't used as enforcement tools.  This is because we may add additional audiences to platform-common in the future, and we want Token Service to still function not knowing what they are.  Consequently, the service can only verbalize new audience names if its platform-common is kept up to date.

The `bans` array will contain all the information related to various bans issued against a player.  Only the longest-lasting ban per permissions is tracked - so if you ban a player from Chat Service indefinitely, then ban them from Chat Service for only a day, the indefinite ban remains and the second one is ignored.

### Permissions

The `Permissions` value of these objects represents the `Audience` enum as a flat integer.  This helps us keep the token's character limit in check and also acts as an agnostic flags system when token-service is behind consuming services on its platform-common version.  When working with .NET and platform-common, you don't need to worry about what these values actually are; just use the `ApiService`'s token generation and the `Audience` enum.  If instead you're working with a project that can't reference this enum, you will need to use DMZ to translate the enum into integer values.  Combine these values with bitwise operations to create a ban spanning more than one `Audience`.

### Token Generation Example

Given the following Audiences (these are not actual values):

|          Field | Binary | Decimal |
|---------------:|:-------|:--------|
|   Chat Service | 0001   | 1       |
|  Token Service | 0010   | 2       |
| Player Service | 0100   | 4       |
|    DMZ Service | 1000   | 8       |

You want a token generation request that grants the player access to all 4 of these systems, so you send the following request:

```
POST /secured/token/generate
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    "screenname": "PostmanTest",
    "discriminator": 3141,
    "origin": "Postman 007",
    "email": "william.maynard@rumbleentertainment.com",
    "days": 5
    "permissions": 15    // This is the bitwise combination of all four Audiences
}
```

Unfortunately, the account has previously been banned from Chat.  You get the following response:

```
{
    "authorization": {
        "created": 1689746383,
        "token": "eyJhb....IBJuQ",
        "expiration": 2005106383,
        "isValid": true,
        "issuer": "Rumble Token Service",
        "origin": "Postman 007"
    },
    "tokenInfo": {
        "aid": "deadbeefdeadbeefdeadbeef",
        "accountId": "deadbeefdeadbeefdeadbeef",
        "audience": [
            "token-service",
            "dmz-service",
            "player-service"
        ],
        "permissions": 14,
        "discriminator": 3141,
        "email": "william.maynard@rumbleentertainment.com",
        "expiration": 2005106383,
        "issuer": "Rumble Token Service",
        "game": "57901c6df82a45708018ba73b8d16004",
        "origin": "Postman 007",
        "screenname": "PostmanTest",
        "username": "PostmanTest#3141",
        "bans": [
            {
                "permissions": 1,
                "expiration": null,
                "reason": "Abusive comments",
                "id": "014f9b17-aef9-4518-89cc-449b41c6953d",
                "audience": [
                    "chat-service"
                ]
            }
        ]
    }
}
```

You'll notice that `tokenInfo.permissions` is 14 - a value less than you asked for - and you have one ban in the array with a `permissions` value of 1.  This means that, because the ban applies to Chat Service, your generated token can not be used there, and consequently was generated without that permission even though you asked for it.

No error is raised during the validation process.

## Platform Authorization

For C# projects, authorization is handled automatically for you by platform-common.  An Authorization Filter is responsible for taking a token, if one is provided, and validating it with Token Service.  This is where the first type of ban will come into play: the Service Ban.

### Service Bans

A Service Ban occurs when the token isn't valid for the Audience of an entire project, as configured during Startup.  The Authorization Filter will reject traffic when a token doesn't contain the respective audience automatically.

### Feature Bans

In addition to Service Bans, we also have Feature Bans.  These represent partial components of our services, and can't be automatically checked by our Authorization Filter.  In order to use a feature ban, you must first add a value to the `Audience` enum.

There are two ways to easily validate tokens for features:

#### Method 1: `RequireAuth` (Preferred)

As of platform-common-1.3.75, you can also use the `RequireAuth` flag on an endpoint or controller with an Audience in addition to the regular `AuthType`.  When specified, this enables the Authorization Filter to handle the traffic rejection as with any other unauthorized request.

Ideally, traffic you need to block can be taken care of by the Authorization Filter before it gets into your endpoint code; this leaves less room for accidentally leaking traffic into endpoint code where requested work can be performed.  This also keeps our responses as a collective platform closer to a standard.

#### Method 2: `TokenInfo.IsValidFor()`

Once you've received a token back from validation, you can call the method `IsValidFor` with an `Audience` parameter to make sure the token has appropriate permissions.  This method returns a boolean and does not break execution with an exception; in this sense, it's more flexible, but you need to make sure you add logic when it returns false.

## Issuing Bans

Bans in token service prior to Bans V2 required a PATCH request and could only result in a full ban of the account.  With V2 and the expanded service / feature bans, this is no longer a possible usage.  Now, the request looks like this:

```
POST /admin/ban
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    // "accountIds": [],
    "ban": {
        "permissions": 1,
        "expiration": 1689749545,
        "reason": "Internal note here"
    }
}
```

Only one ban per `permissions` value is tracked, and those that expire further in the future take precedence.  However, you can technically ban a player twice on the same Audience by using a combined `permissions` with bitwise operations.  Going back to our **Token Generation Example**, if you banned someone with permissions of `1` and `9`, this effectively bans a player _twice_ from Chat.  In order to restore a player's Chat functionality, both of these bans would need to expire or otherwise be removed.

An `expiration` for a ban is a target Unix timestamp and may be omitted.  When this happens, the ban is indefinite.

You can opt in to using `accountIds` to issue the same ban to multiple people at once.  If you use both `accountId` and `accountIds`, only `accountIds` is accepted.

**Important note:** Whenever you issue a ban, make sure you keep the `reason` professional.  Since this is exposed on token validation, it is theoretically possible for a player who has hit our token-service to see the reason.  While it's unlikely, just assume that this is public information.

## Listing Bans

```
GET /admin/status?accountId=deadbeefdeadbeefdeadbeef

Response:
{
    "bans": [
        {
            "permissions": 1,
            "expiration": null,
            "reason": "He's a cheat",
            "id": "014f9b17-aef9-4518-89cc-449b41c6953d",
            "audience": [
                "chat-service"
            ]
        },
        ...
    ]
}
```

## Removing Bans

Pass an array of the bans you wish to remove in the `banIds` field.  If no records are affected, a 400-level response will be returned.

```
PATCH /admin/unban
{
    "accountId": "deadbeefdeadbeefdeadbeef",
    "banIds": [
        "014f9b17-aef9-4518-89cc-449b41c6953d"
    ]
}
```