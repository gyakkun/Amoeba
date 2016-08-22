/// <reference path="typings/index.d.ts" />
"use strict";
var electron = require('electron');
var BrowserWindow = electron.BrowserWindow;
var app = electron.app;
electron.crashReporter.start({ companyName: "Amoeba", submitURL: "" });
var mainWindow = null;
app.on('window-all-closed', function () {
    app.quit();
});
app.on('ready', function () {
    mainWindow = new BrowserWindow({ width: 800, height: 600, 'icon': __dirname + '/images/app.png' });
    mainWindow.loadURL('file://' + __dirname + '/index.html');
    mainWindow.on('closed', function () {
        mainWindow = null;
    });
});
//# sourceMappingURL=main.js.map