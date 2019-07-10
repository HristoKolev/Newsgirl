import { Result } from './api-result';

const networkIsDownMessage = 'Network error, please try again later.';
const serverErrorMessage = 'An error occurred while reading the response from the server.';

const defaultHeaders = {
  'Content-Type': 'application/json',
};

export interface ApiClient {
  send: <TRequest, TResponse = undefined>(
    messageType: string,
    body?: TRequest,
    headers?: { [key: string]: string },
  ) => Promise<Result<TResponse>>;
}

export interface RequestMessage<TRequest> {
  type: string;
  payload: TRequest;
}

export const apiClient = (authToken: string): ApiClient => ({
  send: async <TRequest, TResponse>(type: string, payload: TRequest, headers: { [key: string]: string } = {}): Promise<Result<TResponse>> => {

    const message: RequestMessage<TRequest> = { type, payload };

    const request = {
      method: 'POST',
      headers: {
        ...defaultHeaders,
        ...(authToken ? {Authorization: `JWT ${authToken}`} : {}),
        ...headers,
      },
      body: JSON.stringify(message),
    };

    try {

      const response = await fetch('/api/endpoint', request);

      if (response.status !== 200) {
        return ({errorMessages: [serverErrorMessage], success: false});
      }

      try {
        return await response.json() as Result<TResponse>;
      } catch (e) {
        // Server error. The server didn't return any valid json.
        return ({errorMessages: [serverErrorMessage], success: false});
      }
    } catch (e) {
      // Network error. The request failed.
      return ({errorMessages: [networkIsDownMessage], success: false});
    }
  },
});
