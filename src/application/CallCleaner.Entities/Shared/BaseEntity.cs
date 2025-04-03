namespace CallCleaner.Entities.Shared
{
    public abstract class BaseEntity
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Güncellenme tarihi
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Silinme durumu
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}