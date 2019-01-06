import { TabCom } from './tab-com';

import { isDistributed } from './distribute-helpers';

export const distributeDispatch = new TabCom('distribute-dispatch');

export const distributeMiddleware = () => (next: any) => (action: object) => {

  if (isDistributed(action)) {
    distributeDispatch.sendMessage({
      type: 'dispatch',
      payload: action,
    });
  }

  return next(action);
};
