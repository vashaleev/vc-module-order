angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowUploadController', ['$rootScope', '$scope', 'platformWebApp.dialogService', 'virtoCommerce.contentModule.contentApi', 'FileUploader', 'platformWebApp.bladeNavigationService', function ($rootScope, $scope, dialogService, contentApi, FileUploader, bladeNavigationService) {
        var blade = $scope.blade;
        blade.memberID = (blade.parentBlade.origEntity) ? blade.parentBlade.origEntity.id : blade.parentBlade.parentBlade.origEntity.id;

        blade.isLoading = false;


        function initialize() {
            console.log("initialize");
            if (!$scope.uploader) {
                var uploader = $scope.uploader = new FileUploader({
                    scope: $scope,
                    headers: { Accept: 'application/json' },
                    queueLimit: 1,
                    autoUpload: false,
                    removeAfterUpload: true
                });

                // ADDING FILTERS
                // json only
                uploader.filters.push({
                    name: 'jsonFilter',
                    fn: function (i /*{File|FileLikeObject}*/, options) {
                        return i.name.toLowerCase().endsWith('.json');
                    }
                });

                uploader.onAfterAddingFile = function (item) {
                    console.log(item)
                    item.url = 'api/workflow/upload?name=' + blade.workflowName + '&memberId=' + blade.memberID;
                    uploader.url = 'api/workflow/upload?name=' + blade.workflowName + '&memberId=' + blade.memberID;
                    uploader.uploadAll();
                    blade.isLoading = true;
                };

                uploader.onSuccessItem = function (fileItem, files) {
                    blade.isLoading = false;
                    refreshParentAndClose();
                };

                uploader.onErrorItem = function (item, response, status, headers) {
                    bladeNavigationService.setError(item._file.name + ' failed: ' + (response.message ? response.message : status), blade);
                };
            }
        }

        $scope.setForm = function (form) {
            $scope.formScope = form;
        };

        function refreshParentAndClose() {
           // $scope.bladeClose();
            blade.parentBlade.refresh();
           // $rootScope.$broadcast("cms-statistics-changed", blade.storeId);
        }

        

        blade.title = 'orders.organization-workflow.title',
        blade.headIcon = 'fa-file-text',
        blade.isLoading = false,
        blade.toolbarCommands = [
            {
                name: "Save",
                icon: 'fa fa-save',
                executeMethod: function () {
                    $scope.saveWorkflow();
                },
                permission: 'platform:security:manage'
            }
            ];

        initialize();
    }]);
