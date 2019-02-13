angular.module('virtoCommerce.orderModule')
    .factory('virtoCommerce.orderModule.workflowApi', ['$resource', function ($resource) {
        return $resource('', {}, {
            getOrganizationWorkflows: { method: 'POST', url: 'api/workflow/search' }
        });
    }]);
