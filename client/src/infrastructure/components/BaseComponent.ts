import {Component} from 'react';
import {ComponentUnmountedError, isComponentUnmountedError} from '../errors/ComponentUnmountedError';
import {logErrorEvent} from '../errors/error-logger';

export class BaseComponent<P = {}, S = {}> extends Component<P, S> {
  _isMounted: boolean = false;

  setStateAsync<K extends keyof S>(state: ((prevState: Readonly<S>, props: Readonly<P>) =>
    (Pick<S, K> | S | null)) | (Pick<S, K> | S | null)) {
    return new Promise((resolve) => {
      if (!this._isMounted) {
        throw new ComponentUnmountedError('The operation is being canceled.');
      }
      this.setState(state, resolve);
    });
  }

  unwrapPromise<T>(promise: (...args: any[]) => Promise<T>) {
    return (...args: any[]) => promise(...args).catch((error) => {
      if (!this._isMounted) {
        if (isComponentUnmountedError(error)) {
          console.warn('There was a continuation that executed after the component was unmounted.');
          return;
        } else {
          console.warn('An error occurred in a component that was not mounted.');
          //noinspection JSIgnoredPromiseFromCall
          logErrorEvent({
            message: error.message,
            description: error.stack,
            context: error,
          });
          return;
        }
      }

      this.setState(() => {
        throw error;
      });
    });
  }

  async componentDidMountAsync() {
  }

  async componentWillUnmountAsync() {
  }

  componentDidMount() {
    this._isMounted = true;
    if (this.componentDidMountAsync) {
      this.unwrapPromise(this.componentDidMountAsync.bind(this))();
    }
  }

  componentWillUnmount() {
    this._isMounted = false;
    if (this.componentWillUnmountAsync) {
      this.unwrapPromise(this.componentWillUnmountAsync.bind(this))();
    }
  }
}
