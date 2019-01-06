import { AnyAction } from 'redux';

export interface GeneralError {
  message: string;
  stack: string;
}

export interface ReactRenderingError extends GeneralError {
  componentStack: string;
}

export interface ReducerErrorOptions {
  message: string;
  stack: string;
  reducerName: string;
  invokingAction: AnyAction;
  componentStack: string;
  state: any; // this `any` is on purpose.
}

export interface ReducerError extends Error {
  options: ReducerErrorOptions;
}

export const isReducerError = (error: Error | ReducerError): error is ReducerError => {
  return !!(error as any).isReducerError;
};

class MyReducerError extends Error {
  constructor(message: string, options: ReducerErrorOptions) {
    super(message);
    Object.assign(this, {
      options: {
        ...options,
        message,
      },
      isReducerError: true,
    });
  }
}

export const createReducerError = (message: string, options: ReducerErrorOptions) =>
  new MyReducerError(message, options);

export interface SagaError extends GeneralError {
  sagaName: string;
  action: AnyAction;
}
