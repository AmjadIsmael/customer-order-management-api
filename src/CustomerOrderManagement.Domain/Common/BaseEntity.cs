using System;
using System.Collections.Generic;
using System.Text;

namespace CustomerOrderManagement.Domain.Common
{
   public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
    }
}
