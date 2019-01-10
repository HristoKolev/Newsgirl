import { Component, ErrorInfo } from 'react';
import { isReducerError, ReactRenderingError } from '../errors';
import { DefaultProps, RenderProps } from '../../redux-types';
import { RouterActionCreators } from '../../global.state';
import { ErrorsActionCreators } from '../errors.state';

interface RenderComponentProps {
  error: ReactRenderingError;
  onClose: () => void;
}

interface Props extends RenderProps<RenderComponentProps> {
  actions: RouterActionCreators & ErrorsActionCreators;
  onError: (err: ReactRenderingError) => void;
  redirectPath: string;
}

interface State {
  error: ReactRenderingError | null;
}

export class ErrorBoundaryComponent extends Component<Props, State> {

  state = {
    error: null,
  };

  componentDidCatch(error: Error, info: ErrorInfo) {

    if (isReducerError(error)) {
      this.props.actions.setReducerError({
        ...error.options,
        componentStack: info.componentStack,
      });
      return;
    }

    const errorObj = {
      message: error.message,
      stack: error.stack || '',
      componentStack: info.componentStack,
    };

    this.setState({error: errorObj});
    this.props.onError(errorObj);
  }

  render() {

    const {error} = this.state;

    if (error === null) {
      return this.props.children;
    }

    return this.props.render({
      error,
      onClose: () => {
        this.props.actions.routerPush(this.props.redirectPath);
        this.setState({error: null});
      },
    });
  }

  static defaultProps: DefaultProps<Props> = {
    redirectPath: '/',
  };
}
