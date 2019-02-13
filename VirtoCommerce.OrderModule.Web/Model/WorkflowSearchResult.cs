using System.Collections.Generic;
using VirtoCommerce.Domain.Order.Model;

namespace VirtoCommerce.OrderModule.Web.Model
{
    public class WorkflowSearchResult
    {
        public WorkflowSearchResult()
        {
            Workflows = new List<WorkflowModel>();
        }

        public int TotalCount { get; set; }

        public List<WorkflowModel> Workflows { get; set; }
    }
}
