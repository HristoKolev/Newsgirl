import React from 'react';
import { RouteComponentProps } from 'react-router';
import { Card, CardBody, CardHeader } from 'mdbreact';

import BackButton from '../infrastructure/components/BackButton';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';
import {AppContext} from '../infrastructure/context';
import { FeedDto} from '../dto';
import {BaseComponent} from '../infrastructure/components/BaseComponent';
import { StandardFrame } from '../infrastructure/components/StandardFrame';

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
}

export class DeleteFeedComponent extends BaseComponent<Props, State> {
  state: State = {
    model: undefined,
    loading: false,
    submitLoading: false,
  };

  componentDidMountAsync = async () => {
    const {feedID} = this.props.match.params;
    await this.getItem(Number.parseInt(feedID, 10));
  };

  async getItem(id: number) {

    const {allActions, server} = this.props.context;

    await this.setStateAsync({
      loading: true,
      model: undefined,
    });

    const response = await server.getFeed({id});

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        model: response.payload.item,
      });
    } else {
      await this.setStateAsync({loading: false});
      allActions.errors.setErrors(response.errorMessages);
    }
  }

  async deleteItem(id: number) {

    const {server, allActions} = this.props.context;

    await this.setStateAsync({submitLoading: true});

    const response = await server.deleteFeed({id});

    if (response.success) {
      await this.setStateAsync({submitLoading: false});
      this.props.history.push('/feeds');
    } else {
      allActions.errors.setErrors(response.errorMessages);
      await this.setStateAsync({submitLoading: false});
    }
  }

  deleteFeed = async () => {
    const {feedID} = this.props.match.params;
    await this.deleteItem(Number.parseInt(feedID, 10));
  };

  render() {

    const {model, loading, submitLoading} = this.state;

    if (loading) {
      return <LoadingSpinner>Loading...</LoadingSpinner>;
    }

    if (submitLoading) {
      return <LoadingSpinner>Deleting...</LoadingSpinner>;
    }

    if (model === undefined) {
      return null;
    }

    return (
      <StandardFrame>
        <Card border="warning" className="mb-3">

          <CardHeader color="red">Warning!!!</CardHeader>

          <CardBody>
            <h3>Do you really want to delete: {model.feedName}, ID: ({model.feedID}) ?</h3>

            <hr/>

            <BackButton
              render={(props) =>
                <a {...props} href=""
                   className="btn btn-md btn-warning Ripple-parent">
                  Back
                </a>
              }
            />

            <button
              className="btn btn-md btn-danger Ripple-parent"
              onClick={this.unwrapPromise(this.deleteFeed)}>
              Confirm
            </button>
          </CardBody>
        </Card>
      </StandardFrame>
    );
  }
}
