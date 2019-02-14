using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Domain.Common.Events;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Model.Search;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.OrderModule.Data.Model;
using VirtoCommerce.OrderModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.OrderModule.Data.Services
{
    public class WorkflowServiceImpl : ServiceBase, IWorkflowService, IWorkflowSearchService
    {
        public WorkflowServiceImpl(Func<IOrderRepository> orderRepositoryFactory, IEventPublisher eventPublisher)
        {
            RepositoryFactory = orderRepositoryFactory;
            EventPublisher = eventPublisher;
        }

        protected Func<IOrderRepository> RepositoryFactory { get; }
        protected IEventPublisher EventPublisher { get; }

        public void Delete(string[] ids)
        {
            var workflows = GetByIds(ids);
            using (var repository = RepositoryFactory())
            {
                //Raise domain events before deletion
                var changedEntries = workflows.Select(x => new GenericChangedEntry<WorkflowModel>(x, EntryState.Deleted));
                EventPublisher.Publish(new WorkflowChangeEvent(changedEntries));

                repository.RemoveWorkflowsByIds(ids);
                
                repository.UnitOfWork.Commit();
                //Raise domain events after deletion
                EventPublisher.Publish(new WorkflowChangedEvent(changedEntries));
            }
        }

        public void SaveChanges(WorkflowModel[] workflows)
        {
            var countActive = workflows.Count(x => x.IsActive);

            if (countActive > 1)
            {
                throw new ArgumentException("Too much activated workflows");
            }

            var pkMap = new PrimaryKeyResolvingMap();
            var changedEntries = new List<GenericChangedEntry<WorkflowModel>>();

            using (var repository = RepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                var dataExistWorkflows = repository.GetWorkflows(workflows.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());

                if (countActive > 0)
                {
                    DeactivatePreviousWorkflows(repository);
                }

                foreach (var workflow in workflows)
                {
                    var originalEntity = dataExistWorkflows.FirstOrDefault(x => x.Id == workflow.Id);

                    var modifiedEntity = AbstractTypeFactory<WorkflowEntity>.TryCreateInstance()
                                                                                 .FromModel(workflow, pkMap);
                    if (originalEntity != null)
                    {
                        changeTracker.Attach(originalEntity);
                        var oldEntry = originalEntity.ToModel(AbstractTypeFactory<WorkflowModel>.TryCreateInstance());

                        changedEntries.Add(new GenericChangedEntry<WorkflowModel>(workflow, oldEntry, EntryState.Modified));
                        modifiedEntity?.Patch(originalEntity);
                    }
                    else
                    {
                        repository.Add(modifiedEntity);
                        changedEntries.Add(new GenericChangedEntry<WorkflowModel>(workflow, EntryState.Added));
                    }
                }

                EventPublisher.Publish(new WorkflowChangeEvent(changedEntries));
                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }

            EventPublisher.Publish(new WorkflowChangeEvent(changedEntries));
        }

        public void Update(WorkflowModel changedWorkflow)
        {
            using (var repository = RepositoryFactory())
            {
                var workflow = repository.GetWorkflows(new[] { changedWorkflow.Id }).FirstOrDefault(x => !x.IsDeleted);
                if (workflow == null)
                {
                    throw new ArgumentNullException("No model with this id");
                }

                if (changedWorkflow.IsActive)
                {
                    DeactivatePreviousWorkflows(repository);
                }

                if (!string.IsNullOrWhiteSpace(changedWorkflow.Name))
                {
                    workflow.Name = changedWorkflow.Name;
                }
                workflow.IsActive = changedWorkflow.IsActive;

                CommitChanges(repository);
            }
        }

        public GenericSearchResult<WorkflowModel> SearchWorkflows(WorkflowSearchCriteria criteria)
        {
            using (var repository = RepositoryFactory())
            {
                repository.DisableChangesTracking(); 

                var query = GetWorkflowsQuery(repository, criteria).Where(x => !x.IsDeleted);
                var totalCount = query.Count();

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<WorkflowEntity>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos);

                var workflowIds = query.Select(x => x.Id).Skip(criteria.Skip).Take(criteria.Take).ToArray();
                var workflows = GetByIds(workflowIds);

                var retVal = new GenericSearchResult<WorkflowModel>
                {
                    TotalCount = totalCount,
                    Results = workflows.AsQueryable().OrderBySortInfos(sortInfos).ToList()
                };

                return retVal;
            }
        }

        public virtual WorkflowModel[] GetByIds(string[] workflowIds)
        {
            var retVal = new List<WorkflowModel>();

            using (var repository = RepositoryFactory())
            {
                repository.DisableChangesTracking();

                var workflowEntities = repository.GetWorkflows(workflowIds).Where(x => !x.IsDeleted);
                foreach (var orderEntity in workflowEntities)
                {
                    var workflow = AbstractTypeFactory<WorkflowModel>.TryCreateInstance();
                    if (workflow != null)
                    {
                        workflow = orderEntity.ToModel(workflow);
                        retVal.Add(workflow);
                    }
                }
            }

            return retVal.ToArray();
        }

        protected IQueryable<WorkflowEntity> GetWorkflowsQuery(IOrderRepository repository, WorkflowSearchCriteria criteria)
        {
            var query = repository.Workflows;

            if (criteria.Name != null)
            {
                query = query.Where(x => x.Name == criteria.Name);
            }

            if (criteria.MemberId != null)
            {
                query = query.Where(x => x.MemberId == criteria.MemberId);
            }

            if (criteria.IsActive != null)
            {
                query = query.Where(x => x.IsActive == criteria.IsActive);
            }

            return query;
        }
        
        private void DeactivatePreviousWorkflows(IOrderRepository repository)
        {
            var activatedWorkflows = repository.Workflows.Where(x => x.IsActive).ToList();
            foreach (var activeWorkflow in activatedWorkflows)
            {
                activeWorkflow.IsActive = false;
            }
        }
    }
}
