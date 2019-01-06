import React, { SFC } from 'react';
import { ToastContainer } from 'react-toastify';

import { FooterComponent } from './FooterComponent';
import { Header } from './Header';
import { ChildrenProps } from '../../infrastructure/redux-types';
import GlobalErrorComponent from '../../infrastructure/errors/components/GlobalErrorComponent';
import {
  ErrorMessagesModal,
  ReducerErrorModal,
  SagaErrorModal,
} from '../../infrastructure/errors/components/ErrorComponents';

export const App: SFC<ChildrenProps> = ({children}) => (
  <React.Fragment>
    <section className="main">

      <Header/>

      <main className="main__body">{children}</main>

      <GlobalErrorComponent render={({error, sagaError, reducerError, onClose}) => {
        if (error) {
          return <ErrorMessagesModal onClose={onClose} error={error}/>;
        }
        if (sagaError) {

          return <SagaErrorModal onClose={onClose} sagaError={sagaError}/>;
        }
        if (reducerError) {
          return <ReducerErrorModal onClose={onClose} reducerError={reducerError}/>;
        }
        return null;
      }}/>

      <ToastContainer />

    </section>

    <FooterComponent/>

  </React.Fragment>
);
