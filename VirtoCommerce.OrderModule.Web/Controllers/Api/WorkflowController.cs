using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Model.Search;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.OrderModule.Web.Security;
using VirtoCommerce.Platform.Core.Web.Security;
using WebModel = VirtoCommerce.OrderModule.Web.Model;

namespace VirtoCommerce.OrderModule.Web.Controllers.Api
{
    [RoutePrefix("api/workflow")]
    public class WorkflowController : ApiController
    {
        private readonly IWorkflowService _workflowService;
        private readonly IWorkflowSearchService _searchService;
        private readonly IWorkflowStateMachineService _stateMachineService;

        public WorkflowController(IWorkflowService workflowService, IWorkflowSearchService searchService, IWorkflowStateMachineService stateMachineService)
        {
            _workflowService = workflowService;
            _searchService = searchService;
            _stateMachineService = stateMachineService;
        }

        /// <summary>
        /// Search customer orders by given criteria
        /// </summary>
        /// <param name="criteria">criteria</param>
        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(WebModel.WorkflowSearchResult))]
        [CheckPermission(Permission = WorkflowPredefinedPermissions.Read)]
        public IHttpActionResult Search(WorkflowSearchCriteria criteria)
        {
            var result = _searchService.SearchWorkflows(criteria);
            var retVal = new WebModel.WorkflowSearchResult
            {
                Workflows = result.Results.ToList(),
                TotalCount = result.TotalCount
            };

            return Ok(retVal);
        }


        /// <summary>
        /// Find workflow by id
        /// </summary>
        /// <remarks>Return a single workflow or null if workflow was not found</remarks>
        /// <param name="id">workflow id</param>
        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(WorkflowModel))]
        [CheckPermission(Permission = WorkflowPredefinedPermissions.Read)]
        public IHttpActionResult GetById(string id)
        {
            var retVal = _workflowService.GetByIds(new[] { id }).FirstOrDefault();
            if (retVal == null)
            {
                return NotFound();
            }

            return Ok(retVal);
        }

        /// <summary>
        /// Add new workflow to system
        /// </summary>
        /// <param name="name">name of new workflow</param>
        /// <param name="memberId">organizationId for workflow</param>
        [HttpPost]
        [Route("upload")]
        [ResponseType(typeof(WorkflowModel))]
        [CheckPermission(Permission = WorkflowPredefinedPermissions.Create)]
        public async Task<IHttpActionResult> CreateWorkflow(string name, string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId))
            {
                return BadRequest("OrganizationId can't be null");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name can't be empty");
            }

            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = new MultipartMemoryStreamProvider();

            var file = await Request.Content.ReadAsMultipartAsync(provider);
            var fileString = await file.Contents[0].ReadAsStringAsync();

            _stateMachineService.Validate(fileString);

            var workflow = new WorkflowModel
            {
                Name = name,
                MemberId = memberId,
                Workflow = fileString,
                IsActive = true,
                IsDeleted = false
            };

            _workflowService.SaveChanges(new[] { workflow });

            return Ok(workflow);
        }

        /// <summary>
        ///  Update an existing customer order 
        /// </summary>
        /// <param name="workflow">workflow change model</param>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = WorkflowPredefinedPermissions.Update)]
        public IHttpActionResult Update(WorkflowModel workflow)
        {
            _workflowService.Update(workflow);

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        ///  Delete a workflow (only set IsDeleted = true)
        /// </summary>
        /// <param name="ids">workflow ids for delete</param>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(void))]
        [CheckPermission(Permission = WorkflowPredefinedPermissions.Delete)]
        public IHttpActionResult DeleteWorkflowsByIds([FromUri] string[] ids)
        {
            _workflowService.Delete(ids);

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
