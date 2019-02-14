angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowListController', ['$rootScope', '$scope', 'virtoCommerce.orderModule.workflowApi', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper',
        function ($rootScope, $scope, workflowApi, bladeNavigationService, dialogService, uiGridHelper) {
            $scope.uiGridConstants = uiGridHelper.uiGridConstants;
            var blade = $scope.blade;
            blade.updatePermission = 'content:update';
            blade.title = "orders.organization-workflow.approval-workflow";
            blade.contentType = 'workflows';

            blade.refresh = function () {
                blade.isLoading = true;
                $scope.selectedNodeId = undefined;
                workflowApi.getWorkflows({},
                    function (data) {
                        blade.currentEntities = data.workflows;
                        blade.isLoading = false;
                    },
                    function (error) {
                        bladeNavigationService.setError('Error ' + error.status, blade);
                    }
                );
            }

            $scope.openBladeNew = function () {
                $scope.openDetailsBlade();
            };

            /*$scope.openDetailsBlade = function (node) {
                $scope.selectedNodeId = node && node.name;
                var newBlade = {
                    id: 'addWorkflow',
                    controller: 'virtoCommerce.orderModule.organizationWorkflowUploadController',
                    template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-upload.tpl.html',
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };*/
           
            $scope.setActive = function (data) {
                $scope.selectedNodeId = data.id;
                blade.isLoading = true;
                console.log(data);
                workflowApi.update({ id : data.id, isActive: true },
                    function () { blade.refresh(); },
                    function (error) { bladeNavigationService.setError('Error ' + error.status, blade); }
                );
            };

            $scope.delete = function (data) {
                $scope.selectedNodeId = data.id;
                
                bladeNavigationService.closeChildrenBlades(blade, function () {
                    var dialog = {
                        id: "confirmDelete",
                        title: "orders.organization-workflow.dialogs.title",
                        //message: blade.currentEntities.length > 1 ? "orders.organization-workflow.dialogs.message" : "content.dialogs.theme-delete.message-last-one",
                        message: "orders.organization-workflow.dialogs.message",
                        messageValues: { name: data.name },
                        callback: function (remove) {
                            if (remove) {
                                blade.isLoading = true;
                                workflowApi.deleteWorkflows({ 'ids': [data.id] },
                                    function () { blade.refresh(); },
                                    function (error) { bladeNavigationService.setError('Error ' + error.status, blade); }
                                );
                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                });
            };

            blade.toolbarCommands = [
                {
                    name: "platform.commands.upload", icon: 'fa fa-upload',
                    executeMethod: function () {
                        var newBlade = {
                            id: 'addWorkflow',
                            controller: 'virtoCommerce.orderModule.organizationWorkflowUploadController',
                            template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-upload.tpl.html',
                        };
                        bladeNavigationService.showBlade(newBlade, blade);
                    },
                    canExecuteMethod: function () { return true; },
                    permission: 'content:create'
                }
            ];
           
            // ui-grid
            $scope.setGridOptions = function (gridOptions) {
                uiGridHelper.initialize($scope, gridOptions);
            };

            blade.refresh();
        }
    ]);
