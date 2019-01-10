import { settings, SettingsState } from './settings';

const initialState = settings;

export const settingsReducer = (state = initialState) => state;

declare module './redux-types' {

  interface ReduxState {
    settings: SettingsState;
  }
}
