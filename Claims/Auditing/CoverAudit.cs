namespace Claims.Auditing
{
    public class CoverAudit
    {
        public int Id { get; set; }

        // TODO: Revisit this property. It was optional in the request, but required by the database, so it was made mandatory to get the project running.
        public string CoverId { get; set; }

        public DateTime Created { get; set; }

        // TODO: Revisit this property. It was optional in the request, but required by the database, so it was made mandatory to get the project running.
        public string HttpRequestType { get; set; }
    }
}
