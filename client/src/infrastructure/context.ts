import { ActionCreatorsMapObject, bindActionCreators } from 'redux';
import { AllActionCreators, ReduxState, ReduxStore } from './redux-types';
import { ApiClient } from './api-client';

export interface ComputedAppContext {
  allActions: any; // the `any` here is on purpose
  api: ApiClient;
}

export interface AppContext extends ComputedAppContext {
  store: ReduxStore;
  state: ReduxState;
  allActions: AllActionCreators;
}

export const createContext = (store: ReduxStore): AppContext => {

  const bindActions = (actionCreatorMap: ActionCreatorsMapObject) => {
    return Object.entries(actionCreatorMap)
      .map((x) => ({[x[0]]: bindActionCreators(x[1], store.dispatch)}))
      .reduce((x, y) => Object.assign(x, y), {}) as any;
  };

  const state = store.getState();
  const context = store.initContext(state);

  return {
    ...context,
    allActions: {
      ...bindActions(context.allActions),
    },
    state,
    store,
  };
};

export type ContextObjectInjector = () => AppContext;

export const createContextObjectFactory = (store: ReduxStore): ContextObjectInjector => () => createContext(store);

export interface ContextInjectorProps {
  ctof: ContextObjectInjector;
}
