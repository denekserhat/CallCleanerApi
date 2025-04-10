using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların spam olarak bildirdiği numaraları temsil eden sınıf
    /// </summary>
    public class SpamReport : BaseEntity
    {
        /// <summary>
        /// Kullanıcı tarafından bildiren kişi ID'si
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Spam bildirimi yapılan numara ID'si
        /// </summary>
        public int ReportedNumberId { get; set; }

        /// <summary>
        /// Spam bildirimi yapılan numara
        /// </summary>
        public string PhoneNumberReported { get; set; }

        /// <summary>
        /// Spam bildirimi türü
        /// </summary>
        public SpamType ReportedSpamType { get; set; }

        /// <summary>
        /// Spam bildirimi açıklaması
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Spam bildirimi yapan kişinin bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;

        /// <summary>
        /// Spam bildirimi yapılan numara bilgileri
        /// </summary>
        public virtual ReportedNumber ReportedNumber { get; set; } = null!;
    }

    public enum SpamType
    {
        Telemarketing,
        Scam,
        Annoying,
        Other
    }
}