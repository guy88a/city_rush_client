using System.Collections.Generic;
using UnityEngine;

namespace CityRush.Items
{
    [DisallowMultipleComponent]
    public sealed class Wallet : MonoBehaviour
    {
        private readonly Dictionary<string, int> _balances = new();

        public int Get(string tokenKey)
        {
            if (string.IsNullOrWhiteSpace(tokenKey))
                return 0;

            return _balances.TryGetValue(tokenKey, out int v) ? v : 0;
        }

        public void Add(string tokenKey, int amount)
        {
            if (string.IsNullOrWhiteSpace(tokenKey))
                return;

            if (amount <= 0)
                return;

            _balances.TryGetValue(tokenKey, out int cur);
            _balances[tokenKey] = cur + amount;
        }
    }
}
