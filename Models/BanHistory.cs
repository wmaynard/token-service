using MongoDB.Bson.Serialization.Attributes;
using Rumble.Platform.Common.Models;
using Rumble.Platform.Common.Utilities;
using Rumble.Platform.Common.Utilities.JsonTools;

namespace TokenService.Models;

public class BanHistory : PlatformCollectionDocument
{
    [BsonElement("iat")]
    public long? IssuedAt { get; set; }
    
    [BsonElement("aids")]
    public string[] Accounts { get; set; }
    
    [BsonElement("ban")]
    public Ban Ban { get; set; }
    
    [BsonElement("blame")]
    public TokenInfo Representative { get; set; }

    public BanHistory()
    {
        IssuedAt ??= Timestamp.Now;
    }
}