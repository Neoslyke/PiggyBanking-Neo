using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace PiggyBanking;

[ApiVersion(2, 1)]
public class PiggyBanking : TerrariaPlugin
{
    public override string Name => "PiggyBanking";
    public override string Author => "Neoslyke";
    public override Version Version => new Version(2, 1, 0);
    public override string Description => "Automatically deposits coins to Piggy Bank when using storage items.";

    private static readonly int[] PiggyBankItems = { 87, 3213 };

    public static Configuration Config { get; private set; } = new();

    public PiggyBanking(Main game) : base(game) { }

    public override void Initialize()
    {
        Config = Configuration.Load();

        GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += OnReload;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= OnReload;
        }
        base.Dispose(disposing);
    }

    private void OnReload(TShockAPI.Hooks.ReloadEventArgs args)
    {
        Config = Configuration.Load();
        args.Player?.SendSuccessMessage("[PiggyBanking] Configuration reloaded.");
    }

    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs args)
    {
        if (!Config.Enable) return;

        var player = args.Player;
        if (player == null || !player.Active || !player.IsLoggedIn) return;
        if (!player.HasPermission("piggybanking.use")) return;

        var tPlayer = player.TPlayer;
        var heldItem = tPlayer.inventory[tPlayer.selectedItem];

        if (PiggyBankItems.Contains(heldItem.type) && args.Control.IsUsingItem)
        {
            DepositCoins(player);
        }
    }

    private void DepositCoins(TSPlayer player)
    {
        var tPlayer = player.TPlayer;
        var depositedCoins = new Dictionary<int, int>
        {
            { ItemID.CopperCoin, 0 },
            { ItemID.SilverCoin, 0 },
            { ItemID.GoldCoin, 0 },
            { ItemID.PlatinumCoin, 0 }
        };

        var modifiedInventorySlots = new List<int>();
        var modifiedBankSlots = new List<int>();

        for (int i = 0; i < 54; i++)
        {
            if (i == tPlayer.selectedItem) continue;

            var item = tPlayer.inventory[i];
            if (!IsCoin(item.type)) continue;

            int coinType = item.type;
            int stack = item.stack;

            int added = AddToPiggyBank(tPlayer, coinType, stack, modifiedBankSlots);
            if (added > 0)
            {
                depositedCoins[coinType] += added;
                item.stack -= added;

                if (item.stack <= 0)
                {
                    item.TurnToAir();
                }

                modifiedInventorySlots.Add(i);
            }
        }

        int totalDeposited = depositedCoins.Values.Sum();

        if (totalDeposited == 0)
            return;

        foreach (int slot in modifiedInventorySlots)
        {
            player.SendData(PacketTypes.PlayerSlot, "", player.Index, slot);
        }

        foreach (int slot in modifiedBankSlots)
        {
            player.SendData(PacketTypes.PlayerSlot, "", player.Index, 99 + slot);
        }

        if (Config.ShowMessage)
        {
            string message = BuildDepositMessage(depositedCoins);
            player.SendSuccessMessage($"[PiggyBanking] Deposited {message}");
        }
    }

    private static bool IsCoin(int itemType)
    {
        return itemType == ItemID.CopperCoin ||
               itemType == ItemID.SilverCoin ||
               itemType == ItemID.GoldCoin ||
               itemType == ItemID.PlatinumCoin;
    }

    private static int AddToPiggyBank(Player player, int itemType, int stack, List<int> modifiedSlots)
    {
        int added = 0;

        for (int i = 0; i < 40 && stack > 0; i++)
        {
            var slot = player.bank.item[i];
            if (slot.type == itemType && slot.stack < slot.maxStack)
            {
                int canAdd = Math.Min(stack, slot.maxStack - slot.stack);
                slot.stack += canAdd;
                stack -= canAdd;
                added += canAdd;

                if (!modifiedSlots.Contains(i))
                    modifiedSlots.Add(i);
            }
        }

        for (int i = 0; i < 40 && stack > 0; i++)
        {
            var slot = player.bank.item[i];
            if (slot.type == 0 || slot.stack == 0)
            {
                slot.SetDefaults(itemType);
                int canAdd = Math.Min(stack, slot.maxStack);
                slot.stack = canAdd;
                stack -= canAdd;
                added += canAdd;

                if (!modifiedSlots.Contains(i))
                    modifiedSlots.Add(i);
            }
        }

        return added;
    }

    private static string BuildDepositMessage(Dictionary<int, int> coins)
    {
        var parts = new List<string>();

        if (coins[ItemID.PlatinumCoin] > 0)
            parts.Add($"{coins[ItemID.PlatinumCoin]} platinum");
        if (coins[ItemID.GoldCoin] > 0)
            parts.Add($"{coins[ItemID.GoldCoin]} gold");
        if (coins[ItemID.SilverCoin] > 0)
            parts.Add($"{coins[ItemID.SilverCoin]} silver");
        if (coins[ItemID.CopperCoin] > 0)
            parts.Add($"{coins[ItemID.CopperCoin]} copper");

        return string.Join(", ", parts) + " coin(s).";
    }
}