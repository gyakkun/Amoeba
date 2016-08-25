var webpack = require('webpack');

module.exports = {
  entry: './react/Index.tsx',
  output: {
    path: __dirname + '/src/scripts',
    filename: 'bundle.js',
    publicPath: './',
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
  devtool: 'source-map',
}
