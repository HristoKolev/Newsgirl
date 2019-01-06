const path = require('path');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const autoprefixer = require('autoprefixer');

const postcssPlugin = {
  loader: 'postcss-loader',
  options: {
    plugins: () => [
      autoprefixer({'browsers': ['> 1%', 'last 2 versions']}),
    ],
  },
};

module.exports = {
  context: __dirname,
  entry: ['./src/index.tsx'],
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].bundle.js',
    publicPath: './public/',
  },
  stats: {
    colors: true,
    reasons: true,
    chunks: false,
  },
  resolve: {
    extensions: ['.js', '.ts', '.tsx'],
  },
  plugins: [
    new CopyWebpackPlugin([{from: './public/'}]),
    new MiniCssExtractPlugin(),
  ],
  module: {
    rules: [
      {
        test: /\.[jt]sx?$/,
        use: 'babel-loader',
      },
      {
        test: /\.css$/,
        use: [
          MiniCssExtractPlugin.loader,
          'css-loader',
          postcssPlugin,
        ],
      },
      {
        test: /\.scss$/,
        use: [
          MiniCssExtractPlugin.loader,
          'css-loader',
          'sass-loader',
          postcssPlugin,
        ],
      },
      {
        test: /\.(eot|svg|ttf|woff|woff2)$/,
        use: [{
          loader: 'file-loader',
          options: {
            publicPath: './'
          }
        }]
      },
      {
        test: /\.(png)$/,
        loader: 'file-loader',
      }
    ],
  },
};
