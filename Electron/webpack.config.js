var webpack = require('webpack');

module.exports = {
  entry: './riot/scripts/index.ts',
  output: {
    path: __dirname + '/src/scripts',
    filename: 'bundle.js',
    publicPath: '/src/',
  },
  module: {
    preLoaders: [
      {
        test: /\.tag$/,
        loader: 'riotjs-loader',
      }
    ],
    loaders: [
      {
        test: /\.ts$/,
        loader: 'ts-loader'
      }
    ]
  },
  resolve: {
      extensions: ['', '.js', '.ts', '.tag']
  },
  plugins: [
    new webpack.optimize.UglifyJsPlugin(),
    new webpack.ProvidePlugin({
      riot: 'riot'
    })
  ]
}
