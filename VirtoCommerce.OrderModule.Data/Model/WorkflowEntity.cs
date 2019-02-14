using System;
using System.ComponentModel.DataAnnotations;
using Omu.ValueInjecter;
using VirtoCommerce.Domain.Order.Model;
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

        public WorkflowModel ToModel(WorkflowModel workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            workflow.InjectFrom(this);

            return workflow;
        }

        public WorkflowEntity FromModel(WorkflowModel workflow, PrimaryKeyResolvingMap pkMap)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            pkMap.AddPair(workflow, this);

            this.InjectFrom(workflow);

            return this;
        }

        public virtual void Patch(WorkflowEntity workflow)
        {
            if (workflow == null)
                throw new ArgumentNullException(nameof(workflow));

            workflow.Name = Name;
            workflow.Workflow = Workflow;
            workflow.IsActive = IsActive;
            workflow.IsDeleted = IsDeleted;
            workflow.MemberId = MemberId;
        }
    }
}
