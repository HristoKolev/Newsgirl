import { Action, ActionCreatorsMapObject, Reducer } from 'redux';
import { SagaMap } from '../redux-types';
import { ReducerErrorOptions, SagaError } from './errors';
import { logErrorEvent } from './error-logger';

const actions = {
  SET_ERRORS: 'errors/SET_ERRORS',
  SET_SAGA_ERROR: 'errors/SET_SAGA_ERROR',
  SET_REDUCER_ERROR: 'errors/SET_REDUCER_ERROR',
  CLEAR_ERRORS: 'errors/CLEAR_ERRORS',
};

export interface ErrorsActionCreators extends ActionCreatorsMapObject {
  setErrors: (error: string[]) => Action;
  setSagaError: (error: SagaError) => Action;
  setReducerError: (error: ReducerErrorOptions) => Action;
  clearErrors: () => Action;
}

const actionCreators: ErrorsActionCreators = {
  setErrors: (error) => ({type: actions.SET_ERRORS, error}),
  setSagaError: (error) => ({type: actions.SET_SAGA_ERROR, error}),
  setReducerError: (error) => ({type: actions.SET_REDUCER_ERROR, error}),
  clearErrors: () => ({type: actions.CLEAR_ERRORS}),
};

export const errorsActionCreators = actionCreators;

export interface ErrorsState {
  error: string[] | null;
  sagaError: SagaError | null;
  reducerError: ReducerErrorOptions | null;
}

const initialState: ErrorsState = {
  error: null,
  sagaError: null,
  reducerError: null,
};

export const errorsReducer: Reducer<ErrorsState> = (state = initialState, action) => {
  switch (action.type) {
    case actions.CLEAR_ERRORS: {
      return {
        ...state,
        ...initialState,
      };
    }
    case actions.SET_ERRORS: {
      return {
        ...state,
        ...initialState,
        error: action.error,
      };
    }
    case actions.SET_SAGA_ERROR: {
      return {
        ...state,
        ...initialState,
        sagaError: action.error,

      };
    }
    case actions.SET_REDUCER_ERROR: {
      return {
        ...state,
        ...initialState,
        reducerError: action.error,
      };
    }
    default: {
      return state;
    }
  }
};

export const errorsSagas: SagaMap = {
  [`errors/saga-error-saga`]: {
    actionType: actions.SET_SAGA_ERROR,
    *saga({error}) {

      yield logErrorEvent({
        message: `An error occurred in saga '${error.sagaName}': ${error.message}`,
        description: error.stack,
        context: error,
      });
    },
  },
  [`errors/reducer-error-saga`]: {
    actionType: actions.SET_REDUCER_ERROR,
    *saga({error}) {

      yield logErrorEvent({
        message: `An error occurred in reducer '${error.reducerName}': ${error.message}`,
        description: error.stack,
        context: error,
      });
    },
  },
};

declare module '../redux-types' {

  interface ReduxState {
    errors: ErrorsState;
  }

  interface AllActionCreators {
    errors: ErrorsActionCreators;
  }
}
