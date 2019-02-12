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
            throw new NotImplementedException();
        }

        public void SaveChanges(WorkflowModel[] workflows)
        {
            var pkMap = new PrimaryKeyResolvingMap();
            var changedEntries = new List<GenericChangedEntry<WorkflowModel>>();

            using (var repository = RepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                var dataExistOrders = repository.GetWorkflows(workflows.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());
                foreach (var workflow in workflows)
                {
                    //EnsureThatAllOperationsHaveNumber(order);

                    var originalEntity = dataExistOrders.FirstOrDefault(x => x.Id == workflow.Id);

                    var modifiedEntity = AbstractTypeFactory<WorkflowEntity>.TryCreateInstance()
                                                                                 .FromModel(workflow, pkMap);
                    if (originalEntity != null)
                    {
                        changeTracker.Attach(originalEntity);
                        var oldEntry = originalEntity.ToModel(AbstractTypeFactory<WorkflowModel>.TryCreateInstance());
                        //DynamicPropertyService.LoadDynamicPropertyValues(oldEntry);
                        changedEntries.Add(new GenericChangedEntry<WorkflowModel>(workflow, oldEntry, EntryState.Modified));
                        modifiedEntity?.Patch(originalEntity);
                    }
                    else
                    {
                        repository.Add(modifiedEntity);
                        changedEntries.Add(new GenericChangedEntry<WorkflowModel>(workflow, EntryState.Added));
                    }
                }
                //Raise domain events
                EventPublisher.Publish(new WorkflowChangeEvent(changedEntries));
                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }

            //Save dynamic properties
            //foreach (var workflow in workflows)
            //{
            //    DynamicPropertyService.SaveDynamicPropertyValues(workflow); //What is this shit?
            //}
            //Raise domain events
            EventPublisher.Publish(new WorkflowChangeEvent(changedEntries));
        }

        public GenericSearchResult<WorkflowModel> SearchWorkflows(WorkflowSearchCriteria criteria)
        {
            using (var repository = RepositoryFactory())
            {
                repository.DisableChangesTracking();

                var query = GetWorkflowsQuery(repository, criteria);
                var totalCount = query.Count();

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<WorkflowEntity>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos);

                var workflowIds = query.Select(x => x.Id).Skip(criteria.Skip).Take(criteria.Take).ToArray();
                var workflows = GetByIds(workflowIds); // without response group

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

                var workflowEntities = repository.GetWorkflows(workflowIds);
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

            // Don't return prototypes by default
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
    }
}
