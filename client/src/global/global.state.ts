import { Action, ActionCreatorsMapObject } from 'redux';
import { LocationDescriptor, LocationState } from 'history';
import { push } from 'react-router-redux';
import { SagaMap } from '../infrastructure/redux-types';
import { AppContext } from '../infrastructure/context';
import { toast } from 'react-toastify';

export interface RouterActionCreators extends ActionCreatorsMapObject {
  routerPush: (location: LocationDescriptor, state?: LocationState) => Action;
}

export const routerActionCreators: RouterActionCreators = {
  routerPush: (location, state?) => push(location, state),
};

export interface ToastActionCreators extends ActionCreatorsMapObject {
  error: (message: string) => Action;
}

const toastActions = {
  SHOW_TOAST: 'TOAST/SHOW_TOAST',
};

export const toastActionCreators: ToastActionCreators = {
  error: (message) => ({type: toastActions.SHOW_TOAST, message, toastType: 'error'}),
};

export const toastSagas: SagaMap = {
  [`toast/show-toast`]: {
    actionType: toastActions.SHOW_TOAST,
    * saga({message, toastType}, {}: AppContext) {

      switch (toastType) {
        case 'error': {
          toast.error(message, {position: 'top-right'});
          break;
        }
        case 'success': {
          toast.success(message, {position: 'top-right'});
          break;
        }
        default: {
          throw new Error('Unknown toastType.');
        }
      }

      return 0;
    },
  },
};

declare module '../infrastructure/redux-types' {

  interface AllActionCreators {
    toast: ToastActionCreators;
  }
}
