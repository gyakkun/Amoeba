/// <reference path="../typings/index.d.ts" />

import * as electron from 'electron';

const BrowserWindow = electron.BrowserWindow;
const app = electron.app;

electron.crashReporter.start({companyName: "Amoeba",  submitURL: ""});

var mainWindow: Electron.BrowserWindow = null;

app.on('window-all-closed', () => {
  app.quit();
});

app.on('ready', () => {
  var iconPath = '/src/images/amoeba.png';

  mainWindow = new BrowserWindow({ width: 800, height: 600, 'icon': iconPath});
  mainWindow.loadURL('file://' + __dirname + '/index.html');

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
});
