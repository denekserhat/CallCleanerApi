namespace CallCleaner.Application.Dtos.User
{
    public class BlockUserDTO
    {
        public string Reason { get; set; }
        public DateTime? BlockEndDate { get; set; }
        public string? Notes { get; set; }
    }
}
