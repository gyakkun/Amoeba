var webpack = require('webpack');

module.exports = {
  entry: {
    "main": "./electron/main.ts",
    "scripts/app": "./react/app.tsx"
  },
  module: {
    loaders: [
      {
        test: /\.ts$|\.tsx$/,
        loader: 'ts-loader'
      }
    ]
  },
  resolve: {
      extensions: ['', '.js', '.ts', '.tsx']
  },
  node: {
    __dirname: false,
    __filename: false,
  },
  devtool: 'source-map',
  output: {
    path: __dirname + "/src",
    filename: "[name].js"
  },
  plugins: [
    new webpack.ExternalsPlugin('commonjs', [
      'electron',
    ]),
  ]
}
