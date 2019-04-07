import React from 'react';
import {RouteComponentProps} from 'react-router';
import {Link} from 'react-router-dom';
import {Card, CardBody, CardHeader, Table, TableBody} from 'mdbreact';

import {AppContext} from '../infrastructure/context';
import { BaseComponent } from '../infrastructure/components/BaseComponent';
import { FeedDto, GetFeedRequest, GetFeedResponse } from '../dto';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';
import BackButton from '../infrastructure/components/BackButton';
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
}

export class ViewFeedComponent extends BaseComponent<Props, State> {

  state: State = {
    model: undefined,
    loading: false,
  };

  componentDidMountAsync = async () => {
    const {feedID: feedID} = this.props.match.params;
    await this.getItem(Number.parseInt(feedID, 10));
  };

  async getItem(id: number) {

    const {api, allActions} = this.props.context;

    await this.setStateAsync({
      loading: true,
      model: undefined,
    });

    const response = await api.send<GetFeedRequest, GetFeedResponse>(
      'GetFeedRequest', {id},
    );

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        model: response.payload.item,
      });
    } else {
      allActions.errors.setErrors(response.errorMessages);
      await this.setStateAsync({loading: false});
    }
  }

  render() {

    const {loading, model} = this.state;

    if (loading) {
      return <LoadingSpinner>Loading...</LoadingSpinner>;
    }

    if (model === undefined) {
      return null;
    }

    return (
      <StandardFrame>
        <Card>
          <CardHeader color="red">Feed: {model.feedName}</CardHeader>
          <CardBody>

            <Link
              to={`/feeds/${model.feedID}/edit`}
              className="btn btn-sm btn-default Ripple-parent">
              Edit
            </Link>

            <Link
              to={`/feeds/${model.feedID}/delete`}
              className="btn btn-sm btn-danger Ripple-parent">
              Delete
            </Link>

            <BackButton
              render={(props) =>
                <a {...props} href=""
                   className="btn btn-sm btn-warning Ripple-parent">
                  Back
                </a>}/>
            <hr/>
            <Card border="danger" className="mb-3">
              <CardBody>
                <Table className="detail-table">
                  <TableBody>
                    <tr>
                      <td>ID:</td>
                      <td>{model.feedID}</td>
                    </tr>
                    <tr>
                      <td>Name:</td>
                      <td>{model.feedName}</td>
                    </tr>
                    <tr>
                      <td>URL:</td>
                      <td><a href={model.feedUrl} target="_blank">{model.feedUrl}</a></td>
                    </tr>

                  </TableBody>
                </Table>
              </CardBody>
            </Card>
          </CardBody>
        </Card>
      </StandardFrame>
    );
  }
}
