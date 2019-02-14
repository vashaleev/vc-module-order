angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowLoadWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', function ($scope, bladeNavigationService) {
        var blade = $scope.blade;
        var filter = { take: 0 };

        /*function refresh() {
            $scope.loading = true;

            reviewsApi.search(filter, function (data) {
                $scope.loading = false;
                $scope.totalCount = data.totalCount;
            });
        }*/

        blade.addNewWorkflow = function () {
            var newBlade = {
                id: 'addWorkflow',
                controller: 'virtoCommerce.orderModule.organizationWorkflowUploadController',
                template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-upload.tpl.html'
            };
            bladeNavigationService.showBlade(newBlade, blade);
        };

        blade.openWorkflowsBlade = function () {
            var newBlade = {
                id: 'openWorkflowsList',
                controller: 'virtoCommerce.orderModule.organizationWorkflowListController',
                template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-list.tpl.html'
            };
            bladeNavigationService.showBlade(newBlade, blade);
        };

        $scope.$watch("blade.currentEntityId", function (id) {
            console.log(id);
            /*filter.productIds = [id];

            if (id) refresh();*/
        });
    }]);
