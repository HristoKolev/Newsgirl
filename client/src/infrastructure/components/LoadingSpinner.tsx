import React, { Component } from 'react';
import { ChildrenProps } from '../redux-types';

import './LoadingSpinner.css';

export default class LoadingSpinner extends Component<ChildrenProps> {

  state = {
    show: false,
  };

  unmounted = false;

  componentDidMount() {
    setTimeout(() => {
      if (!this.unmounted) {
        this.setState({show: true});
      }
    }, 500);
  }

  componentWillUnmount() {
    this.unmounted = true;
  }

  render() {

    if (!this.state.show) {
      return null;
    }

    return (
      <div className="loader-wrapper">
        <div className="sk-circle">
          <div className="sk-circle1 sk-child"/>
          <div className="sk-circle2 sk-child"/>
          <div className="sk-circle3 sk-child"/>
          <div className="sk-circle4 sk-child"/>
          <div className="sk-circle5 sk-child"/>
          <div className="sk-circle6 sk-child"/>
          <div className="sk-circle7 sk-child"/>
          <div className="sk-circle8 sk-child"/>
          <div className="sk-circle9 sk-child"/>
          <div className="sk-circle10 sk-child"/>
          <div className="sk-circle11 sk-child"/>
          <div className="sk-circle12 sk-child"/>
        </div>

        <div className="loading-text">{this.props.children}</div>
      </div>
    );
  }
}
