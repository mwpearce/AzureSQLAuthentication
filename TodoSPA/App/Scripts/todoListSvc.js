'use strict';
angular.module('todoApp')
.factory('todoListSvc', ['$http', function ($http) {
    var apiEndpoint = 'https://emntodolistservice.azurewebsites.net/';

    $http.defaults.useXDomain = true;
    delete $http.defaults.headers.common['X-Requested-With'];

    return {
        getItems : function(){
            return $http.get(apiEndpoint + 'api/TodoList');
        },
        postItem : function(item){
            return $http.post(apiEndpoint + 'api/TodoList/',item);
        }
    };
}]);