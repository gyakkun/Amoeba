/// <reference path="../../typings/index.d.ts" />

import electron = require('electron');
const BrowserWindow = electron.BrowserWindow;
const Tray = electron.Tray;
const app = electron.app;

electron.crashReporter.start({companyName: "Amoeba",  submitURL: ""});

var mainWindow: Electron.BrowserWindow = null;
var tray: Electron.Tray = null;

app.on('window-all-closed', () => {
  app.quit();
});

app.on('ready', () => {
  var iconPath = __dirname + '/../images/amoeba.png';

  mainWindow = new BrowserWindow({ width: 800, height: 600, 'icon': iconPath});
  mainWindow.loadURL('file://' + __dirname + '/index.html');

  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  tray = new Tray(iconPath);
  
  tray.on('click', () => {
    mainWindow.restore();
    mainWindow.focus();
  });
});
