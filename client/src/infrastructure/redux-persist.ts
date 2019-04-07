import { ReduxStore, StoreOptions } from './redux-types';

const localStorageKey = 'redux_state';
const localStorageVersionKey = 'redux_state_version';

const localStorageVersion = 1;

// Subscribes to the store and persists some of the state's fields to localStorage.
export const configureStatePersistence = (store: ReduxStore, {persistentFields}: StoreOptions) => {

  store.subscribe(() => {

    const state = store.getState();

    const entries = Object.entries(state || {})
      .filter((x) => persistentFields.includes(x[0]))
      .map((x) => ({[x[0]]: x[1]}));

    const subState = Object.assign({}, entries);

    try {
      localStorage.setItem(localStorageKey, JSON.stringify(subState));
    } catch (e) {
      // ignore
    }
  });
};

const ensureCompatibleVersion = () => {

  const version = localStorage.getItem(localStorageVersionKey) || '0';

  if (Number.parseInt(version, 10) < localStorageVersion) {

    localStorage.removeItem(localStorageKey);
    localStorage.setItem(localStorageVersionKey, localStorageVersion.toString());
  }
};

// Returns the persisted fields from localStorage.
export const getPersistedState = () => {

  try {
    ensureCompatibleVersion();
    const json = localStorage.getItem(localStorageKey) || '{}';
    return JSON.parse(json);
  } catch (e) {
    return {};
  }
};
