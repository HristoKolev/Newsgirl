import React, { SFC } from 'react';
import { Route } from 'react-router-dom';
import SessionComponent from '../global/SessionComponent';
import { LoginComponent} from './LoginComponent';

import { ContextInjectorProps } from '../infrastructure/context';

export const AuthRoutes: SFC<ContextInjectorProps> = ({ctof}) => (
  <SessionComponent isLoggedIn={false}>
    <Route exact path="/login" render={(componentProps) => {
      return (<LoginComponent {...componentProps} context={ctof()}/>);
    }}/>
  </SessionComponent>
);
