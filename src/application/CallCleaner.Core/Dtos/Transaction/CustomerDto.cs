using System.Text.Json.Serialization;

namespace CallCleaner.Core.Dtos.Transaction
{
    public enum CarStatus
    {
        Serviste = 1,
        ParcaBekliyor = 2,
        Hazir = 3,
        BakimiGeldi = 4
    }

    public class CustomerDto
    {

        public int Id { get; set; }

        [JsonPropertyName("nameSurname")]
        public string NameSurname { get; set; }

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("carModel")]
        public string CarModel { get; set; }

        [JsonPropertyName("carStatus")]
        public CarStatus CarStatus { get; set; }

        [JsonPropertyName("yapilanIslem")]
        public string? YapilanIslem { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }





}
