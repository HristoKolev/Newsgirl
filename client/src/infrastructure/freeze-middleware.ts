const deepFreezeStrict = (obj: any) => {

  Object.freeze(obj);

  const hasOwnProp = Object.prototype.hasOwnProperty;

  Object.getOwnPropertyNames(obj).forEach((prop) => {

    if (hasOwnProp.call(obj, prop)

      && (typeof obj === 'function' ? prop !== 'caller' && prop !== 'callee' && prop !== 'arguments' : true)
      && obj[prop] !== null
      && (typeof obj[prop] === 'object' || typeof obj[prop] === 'function')
      && !Object.isFrozen(obj[prop])) {

      deepFreezeStrict(obj[prop]);
    }
  });

  return obj;
};

const freezeStoreState = (store: any) => {

  const state = store.getState();

  if (state !== null && typeof state === 'object') {

    deepFreezeStrict(state);
  }
};

/**
 * Middleware that prevents state from being mutated anywhere in the app.
 */

export const freezeMiddleware = (store: any) => (next: any) => (action: any) => {

  console.log(action);

  freezeStoreState(store);

  try {

    return next(action);

  } finally {

    freezeStoreState(store);
  }
};
