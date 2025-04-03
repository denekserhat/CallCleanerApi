using CallCleaner.Entities.Shared;

namespace CallCleaner.Entities.Concrete
{
    /// <summary>
    /// Kullanıcının kendi rehberinden engellediği numaraları temsil eden sınıf
    /// </summary>
    public class WhitelistEntry : BaseEntity
    {
        /// <summary>
        /// Kullanıcı ID'si
        /// </summary>
        public int UserId { get; private set; }

        /// <summary>
        /// Kullanıcının rehberinde bulunan numara
        /// </summary>
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Kullanıcının rehberindeki numaranın sahibi
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// Engelleyen kullanıcı bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;
    }
}