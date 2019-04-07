import React, { FunctionComponent } from 'react';
import { Route } from 'react-router-dom';
import SessionComponent from '../infrastructure/components/SessionComponent';
import { LoginComponent} from './LoginComponent';

import { ContextInjectorProps } from '../infrastructure/context';

export const AuthRoutes: FunctionComponent<ContextInjectorProps> = ({ctof}) => (
  <SessionComponent isLoggedIn={false}>
    <Route exact path="/login" render={(componentProps) => {
      return (<LoginComponent {...componentProps} context={ctof()}/>);
    }}/>
  </SessionComponent>
);
