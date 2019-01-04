export interface SettingsState {
  sentryDns: string;
}

export const settings: SettingsState = window.__injected_settings__ || {};
