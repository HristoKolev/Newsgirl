import { AppContext } from '../infrastructure/context';
import {
  GetFeedItemsRequest,
  GetFeedItemsResponse,
  FeedItemDto,
} from '../dto';
import { BaseComponent } from '../infrastructure/components/BaseComponent';
import { Card, CardHeader, CardBody, Table, TableHead, TableBody } from 'mdbreact';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';
import React from 'react';

interface Props {
  context: AppContext;
}

interface State {
  listItems: FeedItemDto[];
  loading: boolean;
}

export class FeedItemsComponent extends BaseComponent<Props, State> {

  state: State = {
    loading: false,
    listItems: [],
  };

  async searchItems() {

    const {api, allActions} = this.props.context;

    await this.setStateAsync({
      loading: true,
      listItems: [],
    });

    const response = await api.send<GetFeedItemsRequest, GetFeedItemsResponse>(
      'GetFeedItemsRequest',
      {},
    );

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        listItems: response.payload.items,
      });
    } else {
      allActions.errors.setErrors(response.errorMessages);
      await this.setStateAsync({
        loading: false,
      });
    }
  }

  componentDidMountAsync = async () => {
    await this.searchItems();
  };

  render() {

    const {listItems, loading} = this.state;

    return (
        <Card>
          <CardHeader color="red">Live feed</CardHeader>
          <CardBody>
            <Table responsive className="list-table">
              <TableHead small="" color="red" textWhite>
                <tr>
                  <th>Feed</th>
                  <th>Title</th>
                </tr>
              </TableHead>
              {listItems.length > 0 && <TableBody>
                {listItems.map((item, postIndex) =>
                  <tr key={postIndex}>
                    <td>#{item.feedName}</td>
                    <td>
                      <a target="_blank" href={item.feedItemUrl}>{item.feedItemTitle}</a>
                    </td>
                  </tr>,
                )}
              </TableBody>}
            </Table>

            {loading && <LoadingSpinner>Loading...</LoadingSpinner>}

          </CardBody>
        </Card>
    );
  }
}