using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların spam olarak bildirdiği numaraları temsil eden sınıf
    /// </summary>
    public class ReportedNumber : BaseEntity
    {
        /// <summary>
        /// Kullanıcı tarafından bildirilen numara
        /// </summary>
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Engellenen numaranın risk değeri
        /// </summary>
        public SpamRiskLevel CurrentRiskLevel { get; private set; } = SpamRiskLevel.Unknown;

        /// <summary>
        /// Engellenen numaranın spam türü
        /// </summary>
        public string? CommonSpamType { get; private set; }

        /// <summary>
        /// Engel sayısı
        /// </summary>
        public int ReportCount { get; private set; } = 0;

        /// <summary>
        /// Engel bilgileri
        /// </summary>
        public virtual ICollection<SpamReport> SpamReports { get; private set; } = new HashSet<SpamReport>();

        /// <summary>
        /// Yorum bilgileri
        /// </summary>
        public virtual ICollection<NumberComment> Comments { get; private set; } = new HashSet<NumberComment>();
    }


    public enum SpamRiskLevel
    {
        Unknown,
        Low,
        Medium,
        High,
        Confirmed
    }
}