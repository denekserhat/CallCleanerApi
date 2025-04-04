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
        public int UserId { get; set; }

        /// <summary>
        /// Kullanıcının rehberinde bulunan numara
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Kullanıcının rehberindeki numaranın sahibi
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Engelleyen kullanıcı bilgileri
        /// </summary>
        public virtual AppUser User { get; private set; } = null!;
    }
}