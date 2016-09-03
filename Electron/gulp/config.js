// @file config.js
var webpack = require('webpack')

var dest = './build';
var src = './src';

module.exports = {
    dest: dest,

    copy: [
        {
            src: [
                src + '/index.html',
                src + '/images/**',
            ],
            dest: dest,
            options: { base: src }
        },
        {
            src: [
                './bower_components/bootstrap/dist/css/bootstrap.min.css',
                './bower_components/bootstrap/dist/css/bootstrap-theme.min.css',
            ],
            dest: dest + '/css',
        }        
    ],

    js: {
        src: src + '/scripts/**',
        dest: dest + '/scripts',
        uglify: false
    },

    webpack: {
        entry: {
            'main': src + "/scripts/electron/main.ts",
            'app': src + "/scripts/react/app.tsx"
        },
        output: {
            filename: "[name].js"
        },
        resolve: {
            extensions: ['', '.js', '.ts', '.tsx']
        },
        module: {
            loaders: [
                {
                    test: /\.ts$|\.tsx$/,
                    loader: 'ts-loader'
                }
            ]
        },
        node: {
            __dirname: false,
            __filename: false,
        },
        plugins: [
            new webpack.ExternalsPlugin('commonjs', [
                'electron',
            ]),
        ]
    },

    watch: {
        js: src + '/scripts/**',
        www: src + '/index.html'
    }
}
