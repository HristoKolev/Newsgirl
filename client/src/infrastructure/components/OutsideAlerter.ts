import { Component } from 'react';
import * as ReactDOM from 'react-dom';
import { ChildrenProps, DefaultProps } from '../redux-types';

interface Props extends ChildrenProps {
  onLeave: () => void;
}

export class OutsideAlerter extends Component<Props> {

  componentDidMount() {
    document.addEventListener('click', this.handleClickOutside, true);
  }

  componentWillUnmount() {
    document.removeEventListener('click', this.handleClickOutside, true);
  }

  handleClickOutside = (event: any) => {
    const domNode = ReactDOM.findDOMNode(this);

    if (!domNode || !domNode.contains(event.target)) {
      this.props.onLeave();
    }
  };

  render() {
    return this.props.children;
  }

  static defaultProps: DefaultProps<Props> = {
    onLeave: () => {
    },
  };
}
