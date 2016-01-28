'use strict';
angular.module('todoApp')
.controller('userProfileCtrl', ['$scope', '$location', 'userProfileSvc', 'adalAuthenticationService', function ($scope, $location, userProfileSvc, adalService) {
    $scope.error = "";
    $scope.loadingMessage = "Loading...";
    $scope.userProfile = null;

    $scope.populate = function () {
        userProfileSvc.getItem().success(function (result) {
            $scope.userProfile = result;
            $scope.loadingMessage = "";
        }).error(function (err) {
            $scope.error = err;
            $scope.loadingMessage = "";
        })
    };
}]);