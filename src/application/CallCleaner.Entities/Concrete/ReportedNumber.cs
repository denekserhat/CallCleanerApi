using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların spam olarak bildirdiği numaraları temsil eden sınıf
    /// </summary>
    public class ReportedNumber : BaseEntity
    {
        /// <summary>
        /// Constructor eklendi
        /// </summary>
        public ReportedNumber(string phoneNumber)
        {
            PhoneNumber = phoneNumber;
            // Diğer başlangıç değerleri burada atanabilir
            ReportCount = 0;
            CurrentRiskLevel = SpamRiskLevel.Unknown;
            SpamReports = new HashSet<SpamReport>();
            Comments = new HashSet<NumberComment>();
        }

        /// <summary>
        /// Kullanıcı tarafından bildirilen numara
        /// </summary>
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Engellenen numaranın risk değeri
        /// </summary>
        public SpamRiskLevel CurrentRiskLevel { get; set; }

        /// <summary>
        /// Engellenen numaranın spam türü
        /// </summary>
        public string? CommonSpamType { get; set; }

        /// <summary>
        /// Engel sayısı
        /// </summary>
        public int ReportCount { get; set; } = 0;

        /// <summary>
        /// Yorum bilgileri
        /// </summary>
        public virtual ICollection<SpamReport> SpamReports { get; private set; } = new HashSet<SpamReport>();

        /// <summary>
        /// Yorum bilgileri
        /// </summary>
        public virtual ICollection<NumberComment> Comments { get; private set; } = new HashSet<NumberComment>();

        /// <summary>
        /// İlk bildirim tarihi
        /// </summary>
        public DateTime? FirstReportedDate { get; set; }

        /// <summary>
        /// Son bildirim tarihi
        /// </summary>
        public DateTime? LastReportedDate { get; set; }
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