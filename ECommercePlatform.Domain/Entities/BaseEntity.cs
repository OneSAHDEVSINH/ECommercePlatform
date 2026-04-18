namespace ECommercePlatform.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        public virtual void SetCreatedBy(string createdBy)
        {
            CreatedBy = createdBy;
            CreatedOn = DateTime.Now;
        }

        public virtual void SetModifiedBy(string modifiedBy)
        {
            ModifiedBy = modifiedBy;
            ModifiedOn = DateTime.Now;
        }

        public virtual void MarkAsDeleted(string deletedBy)
        {
            IsDeleted = true;
            IsActive = false;
            SetModifiedBy(deletedBy);
        }

        public virtual void Restore(string restoredBy)
        {
            IsDeleted = false;
            IsActive = true;
            SetModifiedBy(restoredBy);
        }

        public virtual void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}