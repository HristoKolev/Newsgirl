import { Action, ActionCreatorsMapObject, Reducer } from 'redux';
import { distributeAction } from './distribute-helpers';
import { LoginResponse } from './auth-dto';

export const sessionActions = {
  LOGIN: 'session/LOGIN',
  LOGOUT: 'session/LOGOUT',
};

export interface SessionActionCreators extends ActionCreatorsMapObject {
  logout: () => Action;
  login: (payload: LoginResponse) => Action;
}

const actionCreators: SessionActionCreators = {
  logout: () => (distributeAction({type: sessionActions.LOGOUT})),
  login: (payload) => (distributeAction({type: sessionActions.LOGIN, payload})),
};

export const sessionActionCreators = actionCreators;

export type SessionState =
  { token: string; }
  & ({ isLoggedIn: false; username: undefined } | { isLoggedIn: true; username: string; });

const initialState: SessionState = {
  isLoggedIn: false,
  username: undefined,
  token: '',
};

export const sessionReducer: Reducer<SessionState> = (state = initialState, action) => {
  switch (action.type) {
    case sessionActions.LOGIN: {
      return {
        ...state,
        ...action.payload,
        isLoggedIn: true,
      };
    }
    case sessionActions.LOGOUT: {
      return {
        ...state,
        ...initialState,
      };
    }
    default: {
      return state;
    }
  }
};

declare module './redux-types' {

  interface ReduxState {
    session: SessionState;
  }

  interface AllActionCreators {
    session: SessionActionCreators;
  }
}
