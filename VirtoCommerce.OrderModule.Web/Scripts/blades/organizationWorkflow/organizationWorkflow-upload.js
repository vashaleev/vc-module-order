angular.module('virtoCommerce.orderModule')
    .controller('virtoCommerce.orderModule.organizationWorkflowUploadController', ['$rootScope', '$scope', 'platformWebApp.dialogService', 'virtoCommerce.contentModule.contentApi', 'FileUploader', 'platformWebApp.bladeNavigationService', 'platformWebApp.authDataStorage', function ($rootScope, $scope, dialogService, contentApi, FileUploader, bladeNavigationService, authDataStorage) {
        var blade = $scope.blade;
        blade.isAdding = false;
        blade.isLoading = false;
        blade.errors = [];
        blade.errorCount = 0;
        if (!$scope.uploader) {
            var authData = authDataStorage.getStoredData();

            var uploader = $scope.uploader = new FileUploader({
                scope: $scope,
                headers: { Accept: 'application/json', Authorization: 'Bearer ' + authData.token },
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
                blade.isAdding = true;
                blade.fileName = item.file.name;
                //bladeNavigationService.setError(null, blade);
                blade.errors = [];
                blade.errorCount = 0;
            };

            uploader.onSuccessItem = function (fileItem, files) {
                blade.isLoading = false;
                blade.errors = [];
                blade.errorCount = 0;
                refreshParentAndClose();
            };

            uploader.onErrorItem = function (item, response, status, headers) {
                var customResponse = {
                    status: status,
                    statusText: item._file.name + ' upload failed',
                    data: response
                };
                console.log(customResponse.data);
                blade.errors.push(customResponse);
                blade.errorCount = blade.errors.length;
                blade.isLoading = false;
                //bladeNavigationService.setError(customResponse, blade);
            };
        }

        $scope.setForm = function (form) {
            $scope.formScope = form;
        };

        function refreshParentAndClose() {
            $scope.bladeClose();
            blade.parentBlade.refresh();
        }



        blade.title = 'orders.organization-workflow.title',
        blade.headIcon = 'fa-file-text',
        blade.isLoading = false,
        blade.toolbarCommands = [
            {
                name: "Save",
                icon: 'fa fa-save',
                executeMethod: function () {
                    blade.isLoading = true;
                    blade.isAdding = false;
                    var fileQueue = uploader.getNotUploadedItems();
                    if (fileQueue.length) {
                        var uploadFile = fileQueue[fileQueue.length - 1];
                        uploadFile.url = 'api/workflow/upload?name=' + blade.workflowName + '&memberId=' + blade.memberId;
                        uploader.uploadItem(uploadFile);
                    }

                },
                canExecuteMethod: function () {
                    return !!$scope.uploader && $scope.uploader.queue.length;
                },
                permission: 'workflow:create'
            }
            ];

    }]);
