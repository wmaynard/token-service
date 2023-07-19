using Rumble.Platform.Common.Minq;
using Rumble.Platform.Common.Models;
using TokenService.Models;

namespace TokenService.Services;

public class BanHistoryService : MinqService<BanHistory>
{
    public BanHistoryService() : base("banHistory") { }

    public void Store(Transaction transaction, TokenInfo admin, string[] accounts, Ban ban) => mongo
        .WithTransaction(transaction)
        .Insert(new BanHistory
        {
            Accounts = accounts,
            Ban = ban,
            Representative = admin
        });
}