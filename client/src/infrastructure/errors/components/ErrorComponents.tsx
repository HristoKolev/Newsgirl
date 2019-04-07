import React, { FunctionComponent } from 'react';
import { CustomModal } from '../../components/CustomModal';
import { ReactRenderingError, ReducerErrorOptions, SagaError } from '../errors';

import './ErrorComponents.scss';
import { Button } from 'mdbreact';

interface ErrorMessageContainerProps {
  errorMessages: string[];
  className?: string;
}

export const ErrorMessagesContainer: FunctionComponent<ErrorMessageContainerProps> = ({errorMessages}) => {

  if (!errorMessages || !errorMessages.length) {
    return null;
  }

  return (
    <div className="card red text-center z-depth-2">
      <div className="card-body">
        {errorMessages && errorMessages.length && errorMessages.map((errorMessage, i) =>
          <div key={i} className="error-container__message">{errorMessage}</div>,
        )}
      </div>
    </div>
  );
};

interface ErrorMessagesModalProps {
  onClose: () => void;
  error: string[];
}

export const ErrorMessagesModal: FunctionComponent<ErrorMessagesModalProps> = ({onClose, error}) => (
  <CustomModal title={'Error...'} onClose={onClose} className="error-messages-modal">

    <ErrorMessagesContainer errorMessages={error}/>

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

  </CustomModal>
);

interface SagaErrorModalProps {
  onClose: () => void;
  sagaError: SagaError;
}

export const SagaErrorModal: FunctionComponent<SagaErrorModalProps> = ({onClose, sagaError}) => (
  <CustomModal
    title={`An error occurred in saga \r\n \`${sagaError.sagaName}\``}
    onClose={onClose}
    className="error-modal">

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

    <div className="error-modal__panel">
      <div>Saga error: {sagaError.message}</div>
      <pre>{sagaError.stack}</pre>
    </div>

    <div className="error-modal__panel">
      <div>Triggering action</div>
      <pre>{JSON.stringify(sagaError.action, null, '\t')}</pre>
    </div>

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

  </CustomModal>
);

interface ReactErrorComponentProps {
  error: ReactRenderingError;
  onClose: () => void;
}

export const ReactErrorComponent: FunctionComponent<ReactErrorComponentProps> = ({error, onClose}) => (
  <CustomModal
    title={'An error occurred while rendering a react component...'}
    onClose={onClose}
    className="error-modal">

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

    <div className="error-modal__panel">
      <div>JS error: {error.message}</div>
      <pre> {error.stack}</pre>
    </div>

    <div className="error-modal__panel">
      <div>React stack</div>
      <pre>{error.componentStack}</pre>
    </div>

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

  </CustomModal>
);

interface ReducerErrorModalProps {
  reducerError: ReducerErrorOptions;
  onClose: () => void;
}

export const ReducerErrorModal: FunctionComponent<ReducerErrorModalProps> = ({reducerError, onClose}) => (
  <CustomModal
    title={`An error occurred in reducer \`${reducerError.reducerName}\``}
    onClose={onClose}
    className="error-modal">

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

    <div className="error-modal__panel">
      <div>Reducer error: {reducerError.message}</div>
      <pre>{reducerError.stack}</pre>
    </div>

    <div className="error-modal__panel">
      <div>Triggering action</div>
      <pre>{JSON.stringify(reducerError.invokingAction, null, '\t')}</pre>
    </div>

    <div className="error-modal__panel">
      <div>React stack</div>
      <pre>{reducerError.componentStack}</pre>
    </div>

    <Button
      onClick={onClose}
      color="warning"
      block>
      Home
    </Button>

  </CustomModal>
);
