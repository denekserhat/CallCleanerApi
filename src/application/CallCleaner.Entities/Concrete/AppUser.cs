using Microsoft.AspNetCore.Identity;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Uygulama kullanıcı sınıfı
    /// </summary>
    public class AppUser : IdentityUser<int>
    {
        /// <summary>
        /// Kullanıcının adı ve soyadı
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Kullanıcının eklendiği tarih
        /// </summary>
        public DateTime CreatedDate { get; set; }


        /// <summary>
        /// Kullanıcının güncellendiği tarih
        /// </summary>
        public DateTime? UpdateDate { get; set; }

        /// <summary>
        /// Kullanıcının aktiflik durumu
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Kullanıcının bildirim ayarları
        /// </summary>
        public virtual UserSettings? Settings { get; private set; }

        /// <summary>
        /// Kullanıcının kendi engellediği numaralar
        /// </summary>
        public virtual ICollection<WhitelistEntry> WhitelistEntries { get; private set; } = new HashSet<WhitelistEntry>();

        /// <summary>
        /// Kullanıcının engellediği numaralar
        /// </summary>
        public virtual ICollection<BlockedCall> BlockedCalls { get; private set; } = new HashSet<BlockedCall>();

        /// <summary>
        /// Kullanıcının bildirdiği numaralar
        /// </summary>
        public virtual ICollection<SpamReport> SpamReports { get; private set; } = new HashSet<SpamReport>();

        /// <summary>
        /// Kullanıcının yorum yaptığı numaralar
        /// </summary>
        public virtual ICollection<NumberComment> NumberComments { get; private set; } = new HashSet<NumberComment>();
    }
}