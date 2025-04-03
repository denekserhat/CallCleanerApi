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
        public BlockingMode BlockingMode { get; private set; } = BlockingMode.Known; // Varsayılan değer

        /// <summary>
        /// Engelin geçerli olduğu saat dilimi
        /// </summary>
        public WorkingHoursMode WorkingHoursMode { get; private set; } = WorkingHoursMode.TwentyFourSeven;

        /// <summary>
        /// Kullanıcının engelleme başlangıç saati
        /// </summary>
        public TimeOnly? CustomStartTime { get; private set; }

        /// <summary>
        /// Kullanıcının engelleme bitiş saati
        /// </summary>
        public TimeOnly? CustomEndTime { get; private set; }

        /// <summary>
        /// Engel bildirim durumu
        /// </summary>
        public bool NotificationsEnabled { get; private set; } = true;

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