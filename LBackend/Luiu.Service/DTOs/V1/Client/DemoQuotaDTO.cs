namespace Luiu.Service.DTOs.V1.Client
{
    public class DemoQuotaDTO
    {
        public int PlaceSearchLimit { get; set; }
        public int PlaceSearchUsed { get; set; }
        public int RouteComputeLimit { get; set; }
        public int RouteComputeUsed { get; set; }
        public int RouteExternalLegLimit { get; set; }
        public int RouteExternalLegUsed { get; set; }
        public int CreatedTripLimit { get; set; }
        public int CreatedTripUsed { get; set; }
        public int CreatedCollectLimit { get; set; }
        public int CreatedCollectUsed { get; set; }
    }
}
