angular.module('virtoCommerce.orderModule')
    .factory('virtoCommerce.orderModule.workflowApi', ['$resource', function ($resource) {
        return $resource('', {}, {
            deleteWorkflows: { method: 'DELETE', url: 'api/workflow' },
            getWorkflows: { method: 'POST', url: 'api/workflow/search' },
            upload: { method: 'POST', url: 'api/workflow/upload' },
            update: { method: 'PUT', url: 'api/workflow' }
        });
    }]);
