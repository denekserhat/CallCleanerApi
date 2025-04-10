using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların yenileme token'ları için veri modeli
    /// </summary>
    public class UserRefreshToken : BaseEntity
    {
        /// <summary>
        /// Kullanıcı ID'si
        /// </summary>
        public int UserId { get; set; }
        /// <summary>
        /// Yenileme token'ı
        /// </summary>
        public string RefreshToken { get; set; } = null!;
        /// <summary>
        /// Token'ın son kullanma tarihi
        /// </summary>
        public DateTime ExpirationDate { get; set; }
        /// <summary>
        /// Token'ın iptal edilme tarihi
        /// </summary>
        public DateTime? RevokedDate { get; set; }
        /// <summary>
        /// Kullanıcının bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;
    }
}
