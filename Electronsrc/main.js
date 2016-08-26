/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId])
/******/ 			return installedModules[moduleId].exports;
/******/
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			exports: {},
/******/ 			id: moduleId,
/******/ 			loaded: false
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.loaded = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(0);
/******/ })
/************************************************************************/
/******/ ({

/***/ 0:
/***/ function(module, exports, __webpack_require__) {

	/* WEBPACK VAR INJECTION */(function(__dirname) {/// <reference path="../typings/index.d.ts" />
	"use strict";
	var electron = __webpack_require__(425);
	var BrowserWindow = electron.BrowserWindow;
	var app = electron.app;
	electron.crashReporter.start({ companyName: "Amoeba", submitURL: "" });
	var mainWindow = null;
	app.on('window-all-closed', function () {
	    app.quit();
	});
	app.on('ready', function () {
	    var iconPath = __dirname + '/images/amoeba.png';
	    mainWindow = new BrowserWindow({ width: 800, height: 600, 'icon': iconPath });
	    mainWindow.loadURL('file://' + __dirname + '/index.html');
	    mainWindow.on('closed', function () {
	        mainWindow = null;
	    });
	});
	
	/* WEBPACK VAR INJECTION */}.call(exports, "/"))

/***/ },

/***/ 425:
/***/ function(module, exports) {

	module.exports = require("electron");

/***/ }

/******/ });
//# sourceMappingURL=main.js.map