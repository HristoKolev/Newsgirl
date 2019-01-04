import Raven from 'raven-js';
import { settings } from '../settings';

interface LoggedError {
  message: string;
  description: string;
  context: any; // this `any` here is on purpose.
}

const sentryClient = Raven.config(settings.sentryDns);

export const logErrorEvent = async (err: LoggedError) => {

  try {
    sentryClient.captureMessage(err.message, {extra: err});
    console.info(err.message, err.description, err.context);
  } catch (e) {
    // ignore
  }
};
