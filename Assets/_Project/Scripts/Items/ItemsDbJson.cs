namespace CityRush.Items
{
    // Raw ItemsDB JSON payload loaded from Resources.
    // Kept for debugging / inspection.
    public sealed class ItemsDbJson
    {
        public string RawJson { get; }


        public ItemsDbJson(string rawJson)
        {
            RawJson = rawJson ?? string.Empty;
        }
    }
}