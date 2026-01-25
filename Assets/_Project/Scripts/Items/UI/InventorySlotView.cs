using UnityEngine;
using UnityEngine.UI;

namespace CityRush.Items.UI
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotView : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private Image frame;
        [SerializeField] private Image bg;
        [SerializeField] private Image icon;

        public Image Frame => frame;
        public Image Bg => bg;
        public Image Icon => icon;

        public void SetEmpty()
        {
            if (bg != null) bg.gameObject.SetActive(false);
            if (icon != null) icon.gameObject.SetActive(false);
        }

        public void SetNonWeapon(Sprite iconSprite)
        {
            if (bg != null) bg.gameObject.SetActive(false);

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(iconSprite != null);
            }
        }

        public void SetWeapon(Sprite iconSprite, Color rarityColor)
        {
            if (bg != null)
            {
                bg.color = rarityColor;
                bg.gameObject.SetActive(true);
            }

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.gameObject.SetActive(iconSprite != null);
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            // Best-effort auto-wire by child names.
            if (frame == null)
            {
                var t = transform.Find("Frame");
                if (t != null) frame = t.GetComponent<Image>();
            }

            if (bg == null)
            {
                var t = transform.Find("BG");
                if (t != null) bg = t.GetComponent<Image>();
            }

            if (icon == null)
            {
                var t = transform.Find("Icon");
                if (t != null) icon = t.GetComponent<Image>();
            }
        }
#endif
    }
}
