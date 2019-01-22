import React from 'react';
import { RouteComponentProps } from 'react-router';
import { Card, CardBody, CardHeader } from 'mdbreact';

import LoadingSpinner from '../infrastructure/components/LoadingSpinner';
import { PostFormComponent } from './FeedFormComponent';
import { AppContext } from '../infrastructure/context';
import {
  FeedDto, GetFeedRequest, GetFeedResponse, SaveFeedRequest, SaveFeedResponse,

} from './feeds.dto';
import { BaseComponent } from '../infrastructure/components/BaseComponent';

interface RouterParams {
  feedID: string;
}

interface Props extends RouteComponentProps<RouterParams> {
  context: AppContext;
}

interface State {
  model: FeedDto | undefined;
  loading: boolean;
  submitLoading: boolean;
  errorMessages: string[];
}

export class EditFeedComponent extends BaseComponent<Props, State> {
  state: State = {
    model: undefined,
    loading: false,
    submitLoading: false,
    errorMessages: [],
  };

  componentDidMountAsync = async () => {
    const {feedID} = this.props.match.params;
    await this.getItem(Number.parseInt(feedID, 10));
  };

  getItem = async (id: number) => {

    const {api, allActions} = this.props.context;

    await this.setStateAsync({
      loading: true,
      model: undefined,
    });

    const response = await api.send<GetFeedRequest, GetFeedResponse>(
      'GetFeedRequest',
      {id},
    );

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        model: response.payload.item,
      });
    } else {

      allActions.errors.setErrors(response.errorMessages);

      await this.setStateAsync({
        loading: false,
      });
    }
  };

  saveItem = async (model: FeedDto | undefined) => {

    const {api} = this.props.context;

    await this.setStateAsync({
      model,
      submitLoading: true,
    });

    if (!this.state.model) {
      throw new Error('The model submitted from the form is undefined.');
    }

    const response = await api.send<SaveFeedRequest, SaveFeedResponse>(
      'SaveFeedRequest',
      {item: this.state.model},
    );

    if (response.success) {
      await this.setStateAsync({
        submitLoading: false,
      });
      this.props.history.push(`/feeds/${response.payload.id}`);

    } else {
      await this.setStateAsync({
        submitLoading: false,
        errorMessages: response.errorMessages,
      });
    }
  };

  render() {

    const {loading, submitLoading, model, errorMessages} = this.state;

    if (submitLoading) {
      return <LoadingSpinner>Saving...</LoadingSpinner>;
    }

    if (loading) {
      return <LoadingSpinner>Loading...</LoadingSpinner>;
    }
    if (!model) {
      return null;
    }

    return (
      <Card>
        <CardHeader color="red">Feed - Edit</CardHeader>
        <CardBody>
          <PostFormComponent
            model={model}
            errorMessages={errorMessages}
            onSubmit={this.unwrapPromise(this.saveItem)}/>
        </CardBody>
      </Card>
    );
  }
}
