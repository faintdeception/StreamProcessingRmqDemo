using System;
using System.Collections.Generic;
using System.Text;

namespace DomainModel
{
    public class Product
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Emoji { get; set; }
    }
}
