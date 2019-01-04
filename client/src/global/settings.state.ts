import { settings, SettingsState } from '../infrastructure/settings';

const initialState = settings;

export const settingsReducer = (state = initialState) => state;

declare module '../infrastructure/redux-types' {

  interface ReduxState {
    settings: SettingsState;
  }
}
