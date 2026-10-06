using Claims.Validation;
using Docker.DotNet.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace Claims;

public class Cover
{
    [BsonId]
    public string Id { get; set; }

    // TODO: Verify whether this is still needed; the MongoDB driver may not support DateOnly serialization.
    //[BsonDateTimeOptions(DateOnly = true)]
    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    // TODO: Verify whether this is still needed; the MongoDB driver may not support DateOnly serialization.
    // [BsonDateTimeOptions(DateOnly = true)] 
    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("claimType")]
    public CoverType Type { get; set; }

    [BsonElement("premium")]
    public decimal Premium { get; set; }
}

public enum CoverType
{
    Yacht = 0,
    PassengerShip = 1,
    ContainerShip = 2,
    BulkCarrier = 3,
    Tanker = 4
}
