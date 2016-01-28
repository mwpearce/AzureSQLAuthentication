'use strict';
angular.module('todoApp')
.factory('userProfileSvc', ['$http', function ($http) {
    var apiEndpoint = 'https://emntodolistservice.azurewebsites.net/';

    $http.defaults.useXDomain = true;
    delete $http.defaults.headers.common['X-Requested-With'];

    return {
        getItem: function () {
            return $http.get(apiEndpoint + 'api/UserProfile');
        }
    }
}]);