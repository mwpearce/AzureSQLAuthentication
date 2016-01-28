'use strict';
angular.module('todoApp', ['ngRoute','AdalAngular'])
.config(['$routeProvider', '$httpProvider', 'adalAuthenticationServiceProvider', function ($routeProvider, $httpProvider, adalProvider) {

    $routeProvider.when("/Home", {
        controller: "homeCtrl",
        templateUrl: "/App/Views/Home.html",
    }).when("/TodoList", {
        controller: "todoListCtrl",
        templateUrl: "/App/Views/TodoList.html",
        requireADLogin: true
    }).when("/UserData", {
        controller: "userDataCtrl",
        templateUrl: "/App/Views/UserData.html",
    }).when("/UserProfile", {
        controller: "userProfileCtrl",
        templateUrl: "/App/Views/UserProfile.html",
        requireADLogin: true
    }).otherwise({ redirectTo: "/Home" });

    adalProvider.init(
        {
            instance: 'https://login.microsoftonline.com/', 
            tenant: 'eastmanchem.onmicrosoft.com',
            clientId: '947b8c16-0188-486c-bb99-94a89b529da5',
            extraQueryParameter: 'nux=1',
            endpoints: {
                "https://emntodolistservice.azurewebsites.net/": "https://eastmanchem.onmicrosoft.com/emntodolistservice"
            }
            //cacheLocation: 'localStorage', // enable this for IE, as sessionStorage does not work for localhost.
        },
        $httpProvider
        );
   
}]);
