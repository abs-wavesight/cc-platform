namespace Abs.CommonCore.Installer;

public static class Constants
{
    public const string PathEnvironmentVariable = "PATH";

    public const string NugetEnvironmentVariableName = "ABS_NUGET_PASSWORD";
    public const string AbsHeaderValue = "ABS";

    public static class Headers
    {
        public const string ClientTenant = "cc-clienttenantid";
        public const string PartnerTenant = "cc-partnertenantid";
        public const string Tenant = "cc-tenantid";
        public const string Client = "cc-clientid";
        public const string ImoNumber = "cc-imo_number";
        public const string VoyageId = "cc-voyage_id";
        public const string Accept = "Accept";
        public const string ContentType = "Content-Type";
    }

    public static class MediaTypes
    {
        public const string Any = "*/*";

        public const string AnyProtobuf = "*/protobuf";
        public const string Protobuf = "application/protobuf";

        public const string AnyJson = "*/json";
        public const string Json = "application/json";
    }
}
