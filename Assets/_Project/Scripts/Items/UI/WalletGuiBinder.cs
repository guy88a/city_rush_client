using System.Globalization;
using TMPro;
using UnityEngine;

namespace CityRush.Items.UI
{
    [DisallowMultipleComponent]
    public sealed class WalletGuiBinder : MonoBehaviour
    {
        [Header("Refs (optional)")]
        [SerializeField] private TMP_Text amountText;

        [Header("Runtime (optional)")]
        [SerializeField] private PlayerItemsRuntime playerItems;

        [Header("Token Key")]
        [SerializeField] private string coinsTokenKey = "Coins";

        private Wallet _wallet;

        private void Awake()
        {
            EnsureRefs();
        }

        private void OnEnable()
        {
            EnsureRefs();
            Refresh();
        }

        private void OnDisable()
        {
            BindToWallet(null);
        }

        private void EnsureRefs()
        {
            if (playerItems == null)
                playerItems = FindFirstObjectByType<PlayerItemsRuntime>();

            if (amountText == null)
                amountText = FindAmountText(transform);

            BindToWallet(playerItems != null ? playerItems.Wallet : null);
        }

        private void BindToWallet(Wallet newWallet)
        {
            if (_wallet == newWallet)
                return;

            if (_wallet != null)
                _wallet.BalanceChanged -= OnBalanceChanged;

            _wallet = newWallet;

            if (isActiveAndEnabled && _wallet != null)
                _wallet.BalanceChanged += OnBalanceChanged;
        }

        private void OnBalanceChanged(string tokenKey, int newBalance)
        {
            if (!string.Equals(tokenKey, coinsTokenKey, System.StringComparison.Ordinal))
                return;

            SetAmountFromCoins(newBalance);
        }

        private void Refresh()
        {
            if (amountText == null)
                return;

            int coins = (_wallet != null) ? _wallet.Get(coinsTokenKey) : 0;
            SetAmountFromCoins(coins);
        }

        private void SetAmountFromCoins(int coins)
        {
            float dollars = coins / 100f;
            amountText.text = dollars.ToString("0.00", CultureInfo.InvariantCulture) + "$";
        }

        private static TMP_Text FindAmountText(Transform root)
        {
            Transform t = root.Find("Fill_R/Amount");
            if (t == null)
                t = root.Find("WalletGUI/Fill_R/Amount");

            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
    }
}
