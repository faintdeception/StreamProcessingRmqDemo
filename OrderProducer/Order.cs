using System;

namespace DomainModel
{
    public class Order
    {
        public Guid Id { get; set; }
        
        public DateTime? OrderDate {get;set;}
        
        public DateTime? CancelDate { get; set; }
        
        public DateTime? ShipDate { get; set; }
        
        public int Quantity { get; set; }
        
        public int OrderNumber { get; set; }
        
        public Product Product { get; set; }
    }
}
