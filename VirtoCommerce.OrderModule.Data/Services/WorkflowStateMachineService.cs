using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VirtoCommerce.Domain.Order.Model.WorkflowStateMachine;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Platform.Core.Security;

using StateMachine = Stateless.StateMachine<
    VirtoCommerce.Domain.Order.Model.WorkflowStateMachine.State,
    VirtoCommerce.Domain.Order.Model.WorkflowStateMachine.Trigger>;

namespace VirtoCommerce.OrderModule.Data.Services
{
    public class WorkflowStateMachineService : IWorkflowStateMachineService
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowStateMachineService(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        public void Validate(string workflowJson)
        {
            WorkflowStateMachine.Validate(workflowJson);
        }

        public IWorkflowStateMachine CreateStateMachine(string workflowId, string initialState = null)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                throw new ArgumentNullException(nameof(workflowId));

            var workflowJson = LoadWorkflowJson(workflowId);
            
            return new WorkflowStateMachine(workflowJson, initialState);
        }

        private string LoadWorkflowJson(string workflowId)
        {
            return _workflowService.GetByIds(new[] { workflowId })
                ?.FirstOrDefault()
                ?.Workflow;
        }
    }

    internal class WorkflowStateMachine : IWorkflowStateMachine
    {
        private StateMachine _stateMachine;

        public State State { get; private set; }

        public WorkflowStateMachine(string workflowJson, string initialState)
        {
            var workflow = DeserializeWorkflow(workflowJson);

            Validate(workflow, initialState);
            Configure(workflow, initialState);
        }

        public IEnumerable<Trigger> GetPermittedTriggers(ApplicationUserExtended user)
        {
            return _stateMachine.GetPermittedTriggers(user);
        }

        public void Fire(ApplicationUserExtended user, string trigger)
        {
            var triggerWithParam = new StateMachine.TriggerWithParameters<ApplicationUserExtended>(new Trigger(trigger, null));
            _stateMachine.Fire(triggerWithParam, user);
        }

        public static void Validate(string workflowJson)
        {
            Validate(DeserializeWorkflow(workflowJson), null);
        }

        private static WorkflowModel DeserializeWorkflow(string workflowJson)
        {
            return JsonConvert.DeserializeObject<WorkflowModel>(workflowJson);
        }

        private void Configure(WorkflowModel workflow, string initialState)
        {
            var initState = FindInitialState(workflow, initialState);

            if (initialState == null)
                State = new State(initState.Name, initState.Description);

            _stateMachine = new StateMachine(() => State, state => State = new State(state.Name));
            
            var hashStates = workflow.States.ToDictionary(s => s.Name, s => s);
            
            foreach (var state in workflow.States)
            {
                var configuration = _stateMachine.Configure(new State(state.Name, state.Description));
                var isExistTriggers = state.Triggers?.Any() ?? false;
            
                if (isExistTriggers)
                {
                    foreach (var trigger in state.Triggers)
                    {
                        var triggerParam = new StateMachine.TriggerWithParameters<ApplicationUserExtended>(new Trigger(trigger.Name, trigger.ToState, trigger.Description));
            
                        configuration.PermitIf(triggerParam, new State(trigger.ToState, hashStates[trigger.ToState].Description), arg =>
                        {
                            var users = trigger.Users;
                            var roles = trigger.Roles;
                            //
                            //if (_creator != null)
                            //{
                            //    if (users != null && users.Contains("$Creator"))
                            //        users = users.Concat(new[] { _creator.Name });
                            //
                            //    if (roles != null && roles.Contains("$Creator"))
                            //        roles = roles.Concat(new[] { _creator.Role });
                            //}
                            //
                            //if (users != null && users.Contains(arg.Name))
                            //    return true;
                            //
                            //if (roles != null && roles.Contains(arg.Role))
                            //    return true;
                            //
                            //return false;

                            return true;
                        });
                    }
                }
            }
        }

        private static void Validate(WorkflowModel workflow, string initialState)
        {
            if (workflow == null)
                throw new ArgumentException("Model workflow deserialize failed");

            if (workflow.States == null || !workflow.States.Any())
                throw new ArgumentException($"There must be at least one state");

            var stateNames = new HashSet<string>();
            var isInitialOne = false;

            foreach (var state in workflow.States)
            {
                if (string.IsNullOrWhiteSpace(state.Name))
                    throw new ArgumentException($"The field 'name' state is required");

                if (stateNames.Contains(state.Name))
                    throw new ArgumentException($"There can be no duplicate states. Duplicate state: {state.Name}");

                stateNames.Add(state.Name);

                if (state.IsInitial)
                {
                    if (isInitialOne)
                        throw new ArgumentException($"Must be one state with 'isInitial' field. Problem state: {state.Name}");

                    isInitialOne = true;
                }
            }

            if (!isInitialOne)
                throw new ArgumentException($"The initial state must be specified 'isInitial'");

            foreach (var state in workflow.States)
            {
                var isExistsTriggers = state.Triggers?.Any() ?? false;

                if (state.IsFinal)
                {
                    if (isExistsTriggers)
                        throw new ArgumentException($"Final states cannot have triggers. Problem state: {state.Name}");
                }
                else
                {
                    if (!isExistsTriggers)
                        throw new ArgumentException($"In a state there are no triggers. Problem state: {state.Name}");

                    var triggerNames = new HashSet<string>();

                    foreach (var trigger in state.Triggers)
                    {
                        if (string.IsNullOrWhiteSpace(trigger.Name))
                            throw new ArgumentException($"The field 'name' of trigger is required");

                        if (string.IsNullOrWhiteSpace(trigger.ToState))
                            throw new ArgumentException($"The field indicating the transition 'toState' is required. Problem sate: {state.Name}");

                        if (triggerNames.Contains(trigger.Name))
                            throw new ArgumentException($"In the state there can not be duplicate triggers. Problem state: {state.Name}, duplicate trigger: {trigger.Name}");

                        triggerNames.Add(trigger.Name);

                        if (!stateNames.Contains(trigger.ToState))
                            throw new ArgumentException($"In the trigger specified non-existent transition '{trigger.ToState}'. Problem state: {state.Name}, duplicate trigger: {trigger.Name}");

                        if ((trigger.Roles == null || !trigger.Roles.Any()) &&
                            (trigger.Users == null || !trigger.Users.Any()))
                            throw new ArgumentException($"At least one role or user must be specified in the trigger. Problem state: {state.Name}, problem trigger: {trigger.Name}");
                    }
                }
            }

            if (initialState != null && !stateNames.Contains(initialState))
                throw new ArgumentException("Initial state not found");
        }

        private StateModel FindInitialState(WorkflowModel workflow, string initialState)
        {
            if (initialState != null)
                return workflow.States.FirstOrDefault(s => s.Name == initialState) ?? throw new ArgumentException($"Initial State '{initialState}' not found");
            else
                return workflow.States.FirstOrDefault(s => s.IsInitial);
        }

        private class TriggerModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string ToState { get; set; }
            public IEnumerable<string> Roles { get; set; }
            public IEnumerable<string> Users { get; set; }
        }

        private class StateModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public bool IsInitial { get; set; }
            public bool IsFinal { get; set; }
            public IEnumerable<TriggerModel> Triggers { get; set; }
        }

        private class WorkflowModel
        {
            public IEnumerable<StateModel> States { get; set; }
        }
    }
}
