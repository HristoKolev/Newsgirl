import React, { SFC } from 'react';
import { Route, Switch } from 'react-router-dom';
import { ContextInjectorProps } from '../infrastructure/context';
import SessionComponent from '../infrastructure/components/SessionComponent';
import { NewFeedComponent } from './NewModelComponent';
import { ViewFeedComponent } from './ViewFeedComponent';
import { EditFeedComponent } from './EditFeedComponent';
import { DeleteFeedComponent } from './DeleteFeedComponent';
import { FeedListComponent } from './FeedListComponent';

export const FeedsRoutes: SFC<ContextInjectorProps> = ({ctof}) => (
  <SessionComponent>
    <Switch>

      <Route exact path="/feeds" render={(componentProps) => {
        return (<FeedListComponent {...componentProps} context={ctof()}/>);
      }}/>

      <Route exact path="/feeds/new" render={(componentProps) => {
        return (<NewFeedComponent {...componentProps} context={ctof()}/>);
      }}/>

      <Route exact path="/feeds/:feedID" render={(componentProps) => {
        return (<ViewFeedComponent {...componentProps} context={ctof()}/>);
      }}/>

      <Route exact path="/feeds/:feedID/edit" render={(componentProps) => {
        return (<EditFeedComponent {...componentProps} context={ctof()}/>);
      }}/>

      <Route exact path="/feeds/:feedID/delete" render={(componentProps) => {
        return (<DeleteFeedComponent {...componentProps} context={ctof()}/>);
      }}/>

    </Switch>

  </SessionComponent>
);
