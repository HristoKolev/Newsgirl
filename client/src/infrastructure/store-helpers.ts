import { ReduxState } from './redux-types';

export function select<T>(f: (state: ReduxState) => T): (state: ReduxState) => { state: T } {
  return (state) => ({state: f(state)});
}
