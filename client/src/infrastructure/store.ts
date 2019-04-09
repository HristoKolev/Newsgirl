import {
  ActionCreatorsMapObject,
  AnyAction,
  applyMiddleware,
  bindActionCreators,
  combineReducers,
  compose,
  createStore,
  Dispatch,
  Reducer,
  ReducersMapObject,
} from 'redux';
import createSagaMiddleware, { SagaMiddleware } from 'redux-saga';
import { cancel, fork, put, take } from 'redux-saga/effects';
import { AppContext, createContext } from './context';
import { configureStatePersistence, getPersistedState } from './redux-persist';
import { CancellableSagaObject, ReduxStore, SagaMap, SagaObject, StoreOptions } from './redux-types';
import { createReducerError } from './errors/errors';
import { errorsActionCreators } from './errors/errors.state';

/**
 * Runs the sagas.
 * Wraps them in try-catch.
 * Injects them with a 'context' object that gets resolved for each call.
 */
const configureSagas = (sagaMiddleware: SagaMiddleware<any>, sagas: SagaMap, _createContext: () => AppContext) => {

  const isCancellable = (sagaObj: SagaObject | CancellableSagaObject): sagaObj is CancellableSagaObject => {
    return !!(sagaObj as any).cancelActionType;
  };

  for (const [sagaName, sagaObject] of Object.entries(sagas)) {

    sagaMiddleware.run(function* () {

      const sagaWithContext = function* (action: AnyAction) {

        const context = _createContext();

        yield sagaObject.saga(action, context);
      };

      const errorWrappedSaga = function* (action: AnyAction) {

        try {

          yield sagaWithContext(action);

        } catch (e) {

          yield put(errorsActionCreators.setSagaError({
            message: e.message,
            stack: e.stack,
            sagaName,
            action,
          }));

          if (sagaObject.failedActionCreator) {
            yield put(sagaObject.failedActionCreator([e.message]));
          }
        }
      };

      while (true) {

        const action = yield take(sagaObject.actionType);

        let canceledTask: any;

        const workTask = yield fork(function* () {

          yield errorWrappedSaga(action);

          if (canceledTask) {
            yield cancel(canceledTask);
          }
        });

        if (isCancellable(sagaObject)) {

          canceledTask = yield fork(function* () {
            yield take(sagaObject.cancelActionType);
            yield cancel(workTask);
            yield put(sagaObject.canceledActionCreator());
          });
        }
      }
    });
  }
};

/**
 * Wrapper around `combineReducers` from Redux.
 * Logs errors thrown from the reducers.
 */
const configureReducers = (reducerMap: ReducersMapObject): Reducer => {

  const entries = Object.entries(reducerMap)
    .map(([reducerName, reducerFunction]) => [
      reducerName, (state: any, action: AnyAction) => {
        try {
          return reducerFunction(state, action);
        } catch (error) {
          throw createReducerError(error.message, {
            message: error.message,
            stack: error.stack || '',
            reducerName,
            invokingAction: action,
            state,
            componentStack: '',
          });
        }
      }]);

  // @ts-ignore
  const newReducerMap = Object.fromEntries(entries) as ReducersMapObject;

  return combineReducers(newReducerMap);
};

/**
 * Creates and configures the store.
 */
export const configureStore = (options: StoreOptions): ReduxStore => {

  const devTools = window.__REDUX_DEVTOOLS_EXTENSION__ || (() => ((x: any) => x));

  const sagaMiddleware = createSagaMiddleware();

  const enhancer = compose(
    applyMiddleware(
      sagaMiddleware,
      ...options.middleware,
    ),
    devTools(),
  );

  const store = createStore(
    configureReducers(options.reducers),
    getPersistedState(),
    enhancer,
  ) as ReduxStore;

  configureSagas(
    sagaMiddleware,
    options.sagas,
    () => createContext(store),
  );

  configureStatePersistence(store, options);

  store.initContext = options.initContext;

  return store;
};

/**
 * Wrapper around `bindActionCreators` that is hard-coding the actionCreator's name to `actions`
 */
export const wrapActions = (...actionCreators: ActionCreatorsMapObject<any>[]) => (dispatch: Dispatch) => ({
    actions: actionCreators.map((actionCreator) =>
      bindActionCreators(actionCreator, dispatch))
      .reduce((x, y) => Object.assign(x, y), {}) as any,
  }
);
