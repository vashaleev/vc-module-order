angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowListController', ['$rootScope', '$scope', 'virtoCommerce.orderModule.workflowApi', 'virtoCommerce.storeModule.stores', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService', 'platformWebApp.uiGridHelper',
        function ($rootScope, $scope, workflowApi, stores, bladeNavigationService, dialogService, uiGridHelper) {
            $scope.uiGridConstants = uiGridHelper.uiGridConstants;
            var blade = $scope.blade;
            blade.updatePermission = 'content:update';
            blade.title = "orders.widgets.organization-workflow.approval-workflow";
            //blade.contentType = 'workflows';
            blade.defaultWorkflowName = undefined;
            console.log('list');
            console.log(blade);
            blade.refresh = function () {
                blade.isLoading = true;
                $scope.selectedNodeId = undefined;
                workflowApi.getOrganizationWorkflows(null, function (data) {
                    blade.currentEntities = data;
                    console.log(data);
                },
                    function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
            }

            $scope.openBladeNew = function () {
                $scope.openDetailsBlade();
            };

            $scope.openDetailsBlade = function (node) {
                $scope.selectedNodeId = node && node.name;
                var newBlade = {
                    id: 'addWorkflow',
                    controller: 'virtoCommerce.orderModule.organizationWorkflowUploadController',
                    template: 'Modules/$(VirtoCommerce.Orders)/Scripts/blades/organizationWorkflow/organizationWorkflow-upload.tpl.html',
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };
           
            $scope.setActive = function (data) {
                $scope.selectedNodeId = data.name;
                blade.isLoading = true;

                var prop = _.findWhere(blade.store.dynamicProperties, { name: 'DefaultThemeName' });
                prop.values = [{ value: data.name }];

                blade.store.$update(function () {
                    blade.refresh();
                    blade.parentBlade.refresh(blade.storeId, 'defaultTheme', data.name);
                },
                    function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
            };

            $scope.delete = function (data) {
                $scope.selectedNodeId = data.name;
                bladeNavigationService.closeChildrenBlades(blade, function () {
                    var dialog = {
                        id: "confirmDelete",
                        title: "content.dialogs.theme-delete.title",
                        message: blade.currentEntities.length > 1 ? "content.dialogs.theme-delete.message" : "content.dialogs.theme-delete.message-last-one",
                        messageValues: { name: data.name },
                        callback: function (remove) {
                            if (remove) {
                                blade.isLoading = true;
                                contentApi.delete({
                                    contentType: blade.contentType,
                                    storeId: blade.storeId,
                                    urls: [data.url]
                                },
                                    function () {
                                        if (data.name === blade.defaultThemeName) {
                                            var prop = _.findWhere(blade.store.dynamicProperties, { name: 'DefaultThemeName' });
                                            prop.values = [{ value: '' }];

                                            blade.store.$update(function () {
                                                blade.refresh();
                                                $rootScope.$broadcast("cms-statistics-changed", blade.storeId);
                                            },
                                                function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
                                        } else {
                                            blade.refresh();
                                            $rootScope.$broadcast("cms-statistics-changed", blade.storeId);
                                        }
                                    },
                                    function (error) { bladeNavigationService.setError('Error ' + error.status, blade); });
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
