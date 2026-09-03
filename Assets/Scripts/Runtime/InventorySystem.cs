using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokeIdle
{
    public enum InventoryItemId
    {
        Potion = 1,
        SuperPotion = 2
    }

    [Serializable]
    public sealed class InventoryItemStack
    {
        public InventoryItemId ItemId;
        public int Quantity;

        public InventoryItemStack()
        {
        }

        public InventoryItemStack(InventoryItemId itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }

    public sealed class ItemInfo
    {
        public readonly InventoryItemId Id;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int ShopPrice;
        public readonly int HealAmount;

        public ItemInfo(
            InventoryItemId id,
            string displayName,
            string description,
            int shopPrice,
            int healAmount)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            ShopPrice = Mathf.Max(0, shopPrice);
            HealAmount = Mathf.Max(0, healAmount);
        }
    }

    public static class ItemCatalog
    {
        private static readonly Dictionary<InventoryItemId, ItemInfo> Items = BuildCatalog();

        public static ItemInfo Get(InventoryItemId itemId)
        {
            ItemInfo item;
            return Items.TryGetValue(itemId, out item) ? item : null;
        }

        private static Dictionary<InventoryItemId, ItemInfo> BuildCatalog()
        {
            return new Dictionary<InventoryItemId, ItemInfo>
            {
                {
                    InventoryItemId.Potion,
                    new ItemInfo(InventoryItemId.Potion, "Pocao", "Recupera 25 HP.", 25, 25)
                },
                {
                    InventoryItemId.SuperPotion,
                    new ItemInfo(InventoryItemId.SuperPotion, "Super Pocao", "Recupera 60 HP.", 75, 60)
                }
            };
        }
    }

    [Serializable]
    public sealed class PlayerInventory
    {
        public List<InventoryItemStack> Items = new List<InventoryItemStack>();

        public int GetQuantity(InventoryItemId itemId)
        {
            InventoryItemStack stack = Find(itemId);
            return stack == null ? 0 : Mathf.Max(0, stack.Quantity);
        }

        public void Add(InventoryItemId itemId, int quantity)
        {
            if (quantity <= 0 || ItemCatalog.Get(itemId) == null)
            {
                return;
            }

            EnsureValid();
            InventoryItemStack stack = Find(itemId);
            if (stack == null)
            {
                Items.Add(new InventoryItemStack(itemId, quantity));
                return;
            }

            stack.Quantity = Mathf.Max(0, stack.Quantity + quantity);
        }

        public bool TryRemove(InventoryItemId itemId, int quantity)
        {
            if (quantity <= 0)
            {
                return false;
            }

            InventoryItemStack stack = Find(itemId);
            if (stack == null || stack.Quantity < quantity)
            {
                return false;
            }

            stack.Quantity -= quantity;
            if (stack.Quantity == 0)
            {
                Items.Remove(stack);
            }

            return true;
        }

        public void EnsureValid()
        {
            if (Items == null)
            {
                Items = new List<InventoryItemStack>();
            }

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                InventoryItemStack stack = Items[i];
                if (stack == null || stack.Quantity <= 0 || ItemCatalog.Get(stack.ItemId) == null)
                {
                    Items.RemoveAt(i);
                }
            }
        }

        private InventoryItemStack Find(InventoryItemId itemId)
        {
            if (Items == null)
            {
                return null;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] != null && Items[i].ItemId == itemId)
                {
                    return Items[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class LootDropRule
    {
        public InventoryItemId ItemId;
        public float Chance;
        public int MinimumEnemyLevel;

        public LootDropRule(InventoryItemId itemId, float chance, int minimumEnemyLevel)
        {
            ItemId = itemId;
            Chance = Mathf.Clamp01(chance);
            MinimumEnemyLevel = Mathf.Max(1, minimumEnemyLevel);
        }
    }

    public static class LootTable
    {
        private static readonly LootDropRule[] Rules =
        {
            new LootDropRule(InventoryItemId.Potion, 0.22f, 1),
            new LootDropRule(InventoryItemId.SuperPotion, 0.05f, 8)
        };

        public static List<InventoryItemStack> RollDrops(int enemyLevel, int routeNumber)
        {
            var drops = new List<InventoryItemStack>();
            int normalizedEnemyLevel = Mathf.Max(1, enemyLevel);
            float routeBonus = Mathf.Min(0.10f, Mathf.Max(0, routeNumber - 1) * 0.01f);

            for (int i = 0; i < Rules.Length; i++)
            {
                LootDropRule rule = Rules[i];
                if (normalizedEnemyLevel < rule.MinimumEnemyLevel)
                {
                    continue;
                }

                float chance = Mathf.Clamp01(rule.Chance + routeBonus);
                if (UnityEngine.Random.value <= chance)
                {
                    drops.Add(new InventoryItemStack(rule.ItemId, 1));
                }
            }

            return drops;
        }
    }
}
