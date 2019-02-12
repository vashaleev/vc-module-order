using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.OrderModule.Data.Model
{
    public class WorkflowEntity : AuditableEntity
    {
        [Required]
        public string Workflow { get; set; }

        [Required]
        [StringLength(512)]
        public string Name { get; set; }

        [StringLength(128)]
        public string MemberId { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
    }
}
