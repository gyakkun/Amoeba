/// <reference path="typings/index.d.ts" />
var packager = require('electron-packager');
var config = require('./package.json');
var options = {
    dir: './',
    out: './dist',
    name: config.name,
    platform: 'win32',
    arch: 'x64',
    version: '1.3.3',
    icon: './images/app.png',
    'app-version': config.version,
    'version-string': {
        CompanyName: '',
        FileDescription: '',
        OriginalFilename: config.name,
        FileVersion: config.version,
        ProductVersion: config.version,
        ProductName: config.name,
        InternalName: config.name
    },
    overwrite: true,
    asar: true,
    prune: true,
    ignore: new RegExp("node_modules/(electron-packager|electron-prebuilt|\.bin)|typings|release\.js"),
};
packager(options, function (err, appPath) {
    if (err) {
        throw err;
    }
    console.log('Done');
});
//# sourceMappingURL=build.js.map