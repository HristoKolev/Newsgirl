import { AppContext } from '../infrastructure/context';

import {
  GetFeedItemsRequest,
  GetFeedItemsResponse,
  FeedItemDto, FeedDto,
} from '../dto';

import { BaseComponent } from '../infrastructure/components/BaseComponent';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';
import React from 'react';

import './FeedItemsComponent.scss';
import { WideFrame } from '../infrastructure/components/WideFrame';

interface Props {
  context: AppContext;
}

interface State {
  feedItems: FeedItemDto[];
  feeds: FeedDto[];
  loading: boolean;
}

export class FeedItemsComponent extends BaseComponent<Props, State> {

  state: State = {
    loading: false,
    feedItems: [],
    feeds: [],
  };

  async searchItems() {

    const {api, allActions} = this.props.context;

    await this.setStateAsync({
      loading: true,
      feedItems: [],
    });

    const response = await api.send<GetFeedItemsRequest, GetFeedItemsResponse>(
      'GetFeedItemsRequest',
      {},
    );

    if (response.success) {
      await this.setStateAsync({
        loading: false,
        feedItems: response.payload.feedItems,
        feeds: response.payload.feeds,
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

    const {feedItems, feeds, loading} = this.state;

    return (
        <WideFrame>
          <div className="feed-items">
            <div className="feed-items__sidebar">
              {feeds.map((item, postIndex) =>
                <div className="feed-items__feed-row" key={postIndex}>
                  {item.feedName}
                </div>,
              )}
            </div>
            <div className="feed-items__item-list">
              {feedItems.map((item, postIndex) =>
                <div className="feed-items__item-row" key={postIndex}>
                  <a target="_blank" href={item.feedItemUrl}>{item.feedItemTitle}</a>
                </div>,
              )}
              {loading && <LoadingSpinner>Loading...</LoadingSpinner>}
            </div>
          </div>
        </WideFrame>
    );
  }
}
