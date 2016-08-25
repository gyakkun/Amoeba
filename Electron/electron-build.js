var packager = require('electron-packager');
var config = require('./package.json');

var options = {
    dir: './src',
    out: './dist',
    name: 'electron',
    platform: 'linux',
    arch: 'x64',
    version: '1.3.3',
    icon: './src/images/amoeba.png',
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
};

packager(options, function (err, appPath) {
    if (err) {
        throw err;
    }
    console.log('Done');
});
