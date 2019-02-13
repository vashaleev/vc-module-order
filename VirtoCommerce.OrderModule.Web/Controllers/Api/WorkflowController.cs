using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Model.Search;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.OrderModule.Web.Security;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Assets;
using VirtoCommerce.Platform.Core.Web.Security;
using webModel = VirtoCommerce.OrderModule.Web.Model;

namespace VirtoCommerce.OrderModule.Web.Controllers.Api
{
    [RoutePrefix("api/workflow")]
    public class WorkflowController : ApiController
    {
        private readonly IWorkflowService _workflowService;
        private readonly IWorkflowSearchService _searchService;
        private readonly ISecurityService _securityService;
        private readonly IPermissionScopeService _permissionScopeService;

        public WorkflowController(IWorkflowService workflowService, IWorkflowSearchService searchService,
                                  ISecurityService securityService, IPermissionScopeService permissionScopeService)
        {
            _workflowService = workflowService;
            _searchService = searchService;
            _securityService = securityService;
            _permissionScopeService = permissionScopeService;
        }

        /// <summary>
        /// Search customer orders by given criteria
        /// </summary>
        /// <param name="criteria">criteria</param>
        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(webModel.WorkflowSearchResult))]
        public IHttpActionResult Search(WorkflowSearchCriteria criteria)
        {
            //Scope bound ACL filtration
            criteria = FilterWorkflowSearchCriteria(HttpContext.Current.User.Identity.Name, criteria);

            var result = _searchService.SearchWorkflows(criteria);
            var retVal = new webModel.WorkflowSearchResult
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
        public IHttpActionResult GetById(string id)
        {
            var retVal = _workflowService.GetByIds(new[] { id }).FirstOrDefault();
            if (retVal == null)
            {
                return NotFound();
            }

            var scopes = _permissionScopeService.GetObjectPermissionScopeStrings(retVal).ToArray();
            if (!_securityService.UserHasAnyPermission(User.Identity.Name, scopes, OrderPredefinedPermissions.Read))
            {
                throw new HttpResponseException(HttpStatusCode.Unauthorized);
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
        //[CheckPermission(Permission = WorkflowPredefinedPermissions)]
        public async Task<IHttpActionResult> CreateWorkflow(string name, string memberId)
        {
            if (memberId == null)
            {
                return BadRequest("OrganizationId can't be null");
            }

            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = new MultipartMemoryStreamProvider();

            var file = await Request.Content.ReadAsMultipartAsync(provider);

            name = file.Contents[0].Headers.Where(x => x.Key == "Content Type").ToString();

            var fileString = await file.Contents[0].ReadAsStringAsync();

            var workflow = new WorkflowModel
            {
                Name = name,
                MemberId = memberId,
                Workflow = fileString,
                IsActive = true,
                IsDeleted = false
            };

            _workflowService.SaveChanges(new[] { workflow });

            return Ok(name);
        }

        /// <summary>
        ///  Update a existing customer order 
        /// </summary>
        /// <param name="workflow">customer order</param>
        [HttpPut]
        [Route("")]
        [ResponseType(typeof(void))]
        // check permission
        public IHttpActionResult Update(string workflow)
        {

            //_customerOrderService.SaveChanges(new[] { customerOrder });

            return StatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        ///  Delete a workflow (only set IsDeleted = true)
        /// </summary>
        /// <param name="ids">workflow ids for delete</param>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(void))]
        //[CheckPermission(Permission = OrderPredefinedPermissions.Delete)]
        public IHttpActionResult DeleteOrdersByIds([FromUri] string[] ids)
        {
            _workflowService.Delete(ids);
            return StatusCode(HttpStatusCode.NoContent);
        }


        //TODO: ТУТ КАКАЯ-ТО ХУЙНЯ!
        private WorkflowSearchCriteria FilterWorkflowSearchCriteria(string userName, WorkflowSearchCriteria criteria)
        {
            if (!_securityService.UserHasAnyPermission(userName, null, OrderPredefinedPermissions.Read))
            {
                //Get defined user 'read' permission scopes
                var readPermissionScopes = _securityService.GetUserPermissions(userName)
                    .Where(x => x.Id.StartsWith(OrderPredefinedPermissions.Read))
                    .SelectMany(x => x.AssignedScopes)
                    .ToList();
            }

            return criteria;
        }
    }
}
