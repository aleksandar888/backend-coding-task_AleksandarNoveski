using Claims.Validation;
using MongoDB.Bson.Serialization.Attributes;

namespace Claims.Models
{
    public class Claim
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("coverId")]
        public string CoverId { get; set; }

        [BsonElement("created")]

        // TODO: Verify whether this is still needed; the MongoDB driver may not support DateOnly serialization.
        // [BsonDateTimeOptions(DateOnly = true)]
        public DateTime Created { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("claimType")]
        public ClaimType Type { get; set; }

        [BsonElement("damageCost")]
        [MaxDamageCost]
        public decimal DamageCost { get; set; }
    }

    public enum ClaimType
    {
        Collision = 0,
        Grounding = 1,
        BadWeather = 2,
        Fire = 3
    }
}
