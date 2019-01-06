export function distributeAction<T>(action: T): T {

  const newAction = {};

  Object.defineProperty(newAction, '__distribute__', {
    configurable: false,
    enumerable: false,
    value: true,
    writable: false,
  });

  Object.assign(newAction, action);

  return newAction as T;
}

export const isDistributed = (action: any) => action.__distribute__;
