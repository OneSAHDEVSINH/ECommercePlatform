namespace ECommercePlatform.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid UserId { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public Guid? ShippingAddressId { get; set; }
        public Guid? CouponId { get; set; }

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ShippingAddress? ShippingAddress { get; set; }
        public virtual Coupon? Coupon { get; set; }
        public virtual ICollection<OrderItem>? OrderItems { get; set; }

        private Order() { }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Refunded
    }
}