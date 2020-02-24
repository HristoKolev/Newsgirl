import React, { FunctionComponent } from 'react';
import { Route } from 'react-router-dom';
import { ContextInjectorProps } from '../infrastructure/context';
import SessionComponent from '../infrastructure/components/SessionComponent';
import { FeedItemsComponent } from './FeedItemsComponent';

export const FeedItemsRoutes: FunctionComponent<ContextInjectorProps> = ({ctof}) => (
  <SessionComponent>

    <Route exact path="/feed-items" render={(componentProps) => {
      return (<FeedItemsComponent {...componentProps} context={ctof()}/>);
    }}/>

  </SessionComponent>
);
