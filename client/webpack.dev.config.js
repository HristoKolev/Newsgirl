const merge = require('webpack-merge');

module.exports = merge(require('./webpack.base.config'), {
  mode: 'development',
  devtool: 'cheap-module-source-map',
  plugins: [],
});
