using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Consumables
{
    public enum ConsumableType
    {
        TapeFrictionPatch,
        EraserBlocker
    }

    [CreateAssetMenu(menuName = "Paper Football/Roguelike/Consumable", fileName = "Consumable")]
    public class ConsumableDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "consumable";
        [SerializeField] private string displayName = "Consumable";
        [SerializeField] private ConsumableType type;
        [SerializeField] private int maximumUses = 1;

        public string StableId => string.IsNullOrWhiteSpace(stableId) ? name : stableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? StableId : displayName;
        public ConsumableType Type => type;
        public int MaximumUses => Mathf.Max(0, maximumUses);

        public void Configure(string id, string consumableName, ConsumableType consumableType, int maxUses)
        {
            stableId = id;
            displayName = consumableName;
            type = consumableType;
            maximumUses = Mathf.Max(0, maxUses);
        }
    }

    [Serializable]
    public sealed class ConsumableInventory
    {
        [SerializeField] private List<ConsumableStack> stacks = new();

        public IReadOnlyList<ConsumableStack> Stacks => stacks;

        public int GetCount(string stableId)
        {
            ConsumableStack stack = stacks.FirstOrDefault(item => item.stableId == stableId);
            return stack != null ? stack.count : 0;
        }

        public void Add(string stableId, int count)
        {
            if (string.IsNullOrWhiteSpace(stableId) || count <= 0)
            {
                return;
            }

            ConsumableStack stack = stacks.FirstOrDefault(item => item.stableId == stableId);
            if (stack == null)
            {
                stacks.Add(new ConsumableStack(stableId, count));
            }
            else
            {
                stack.count += count;
            }
        }

        public bool TryUse(string stableId)
        {
            ConsumableStack stack = stacks.FirstOrDefault(item => item.stableId == stableId);
            if (stack == null || stack.count <= 0)
            {
                return false;
            }

            stack.count--;
            return true;
        }
    }

    [Serializable]
    public sealed class ConsumableStack
    {
        public string stableId;
        public int count;

        public ConsumableStack(string id, int amount)
        {
            stableId = id;
            count = Mathf.Max(0, amount);
        }
    }

}
