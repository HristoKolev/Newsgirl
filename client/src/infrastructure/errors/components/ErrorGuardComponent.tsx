import React, { SFC } from 'react';
import { connect } from 'react-redux';
import { ErrorBoundaryComponent } from './ErrorBoundaryComponent';
import { ReactErrorComponent } from './ErrorComponents';
import { wrapActions } from '../../store';
import { routerActionCreators } from '../../global.state';
import { errorsActionCreators } from '../errors.state';
import { logErrorEvent } from '../error-logger';

const mapDispatchToProps = wrapActions(routerActionCreators, errorsActionCreators);

const ErrorComponent = connect(null, mapDispatchToProps)(ErrorBoundaryComponent);

const Component: SFC = ({children}) => (
  <ErrorComponent
    onError={(error) => logErrorEvent({
      message: error.message,
      description: error.stack,
      context: error,
    })}
    redirectPath={'/'}
    render={(props) => <ReactErrorComponent {...props} />}>
    {children}
  </ErrorComponent>
);

export default Component;
