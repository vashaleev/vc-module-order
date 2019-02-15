angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowLoadWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
        var blade = $scope.blade;
        
        blade.addNewWorkflow = function () {
            var newBlade = {
                id: 'addWorkflow',
                memberId: this.currentEntityId,
                controller: 'virtoCommerce.orderModule.organizationWorkflowUploadController',
                template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-upload.tpl.html'
            };
            bladeNavigationService.showBlade(newBlade, blade);
        };

        blade.openWorkflowsBlade = function () {
            var newBlade = {
                id: 'openWorkflowsList',
                memberId: this.currentEntityId,
                controller: 'virtoCommerce.orderModule.organizationWorkflowListController',
                template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-list.tpl.html'
            };
            bladeNavigationService.showBlade(newBlade, blade);
        };

        $scope.$watch("blade.currentEntityId", function () { });
        
    }]);
