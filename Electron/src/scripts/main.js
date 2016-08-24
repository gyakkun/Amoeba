/// <reference path="../../typings/index.d.ts" />
"use strict";
var electron = require('electron');
var BrowserWindow = electron.BrowserWindow;
var Tray = electron.Tray;
var app = electron.app;
electron.crashReporter.start({ companyName: "Amoeba", submitURL: "" });
var mainWindow = null;
var tray = null;
app.on('window-all-closed', function () {
    app.quit();
});
app.on('ready', function () {
    var iconPath = __dirname + '/../images/amoeba.png';
    mainWindow = new BrowserWindow({ width: 800, height: 600, 'icon': iconPath });
    mainWindow.loadURL('file://' + __dirname + '/index.html');
    mainWindow.on('closed', function () {
        mainWindow = null;
    });
    mainWindow.on('show', function () {
        if (tray != null) {
            tray.destroy();
            tray = null;
        }
    });
    mainWindow.on('minimize', function () {
        mainWindow.hide();
        tray = new Tray(iconPath);
        tray.on('click', function () {
            mainWindow.show();
        });
    });
});
