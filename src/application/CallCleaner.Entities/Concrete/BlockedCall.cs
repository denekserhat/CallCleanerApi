using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Engellenen çağrılar için veri modeli
    /// </summary>
    public class BlockedCall : BaseEntity
    {
        /// <summary>
        /// Engelleyen kişinin ID'si
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Engellenen numara
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Engellenen numaranın türü (Algılanan veya Raporlanan)
        /// </summary>
        public string? CallType { get; set; }

        /// <summary>
        /// Yanlış engel bildirimi durumu
        /// </summary>
        public bool ReportedAsIncorrect { get; set; } = false;

        /// <summary>
        /// Engelleyen kişinin bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;
    }
}