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
        public int UserId { get; private set; }

        /// <summary>
        /// Engellenen numara
        /// </summary>
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Engellenen numaranın türü (Algılanan veya Raporlanan)
        /// </summary>
        public string? CallType { get; private set; }

        /// <summary>
        /// Yanlış engel bildirimi durumu
        /// </summary>
        public bool ReportedAsIncorrect { get; private set; } = false;

        /// <summary>
        /// Engelleyen kişinin bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;
    }
}