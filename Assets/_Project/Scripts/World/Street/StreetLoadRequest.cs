namespace CityRush.World.Street
{
    public sealed class StreetLoadRequest
    {
        public readonly string StreetId;
        public readonly string StreetJson;

        public StreetLoadRequest(string streetId, string streetJson)
        {
            StreetId = streetId;
            StreetJson = streetJson;
        }
    }
}
