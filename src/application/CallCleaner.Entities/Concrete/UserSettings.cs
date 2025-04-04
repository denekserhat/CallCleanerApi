using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcı ayarlarını temsil eden sınıf
    /// </summary>
    public class UserSettings : BaseEntity
    {
        /// <summary>
        /// Kullanıcı ID'si
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Kullanıcının engelleme modu
        /// </summary>
        public BlockingMode BlockingMode { get; set; } = BlockingMode.Known;

        /// <summary>
        /// Engelin geçerli olduğu saat dilimi
        /// </summary>
        public WorkingHoursMode WorkingHoursMode { get; set; } = WorkingHoursMode.TwentyFourSeven;

        /// <summary>
        /// Kullanıcının engelleme başlangıç saati
        /// </summary>
        public TimeOnly? CustomStartTime { get; set; }

        /// <summary>
        /// Kullanıcının engelleme bitiş saati
        /// </summary>
        public TimeOnly? CustomEndTime { get; set; }

        /// <summary>
        /// Engel bildirim durumu
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        public virtual AppUser User { get; private set; } = null!;
    }


    public enum BlockingMode
    {
        All,
        Known,
        Custom
    }

    public enum WorkingHoursMode
    {
        TwentyFourSeven,
        Custom
    }
}