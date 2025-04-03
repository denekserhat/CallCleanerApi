using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcıların numaralar hakkında yaptığı yorumları temsil eden sınıf
    /// </summary>
    public class NumberComment : BaseEntity
    {
        /// <summary>
        /// Yorumu yapan kullanıcının ID'si
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Yorumun yapıldığı numara
        /// </summary>
        public int ReportedNumberId { get; private set; }

        /// <summary>
        /// Yorum
        /// </summary>
        public string CommentText { get; private set; }

        /// <summary>
        /// Yorum yapan kişinin bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;

        /// <summary>
        /// Yorum yapılan numara
        /// </summary>
        public virtual ReportedNumber ReportedNumber { get; private set; } = null!;
    }
}