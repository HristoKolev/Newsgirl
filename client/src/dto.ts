import { Result } from './infrastructure/api-result';
import { ApiClient } from './infrastructure/api-client';

export interface GetFeedItemsRequest {

}

export interface DeleteFeedRequest {
  id: number;
  cats?: number;
}

export interface NewFeedRequest {

}

export interface GetFeedRequest {
  id: number;
}

export interface SaveFeedRequest {
  item: FeedDto;
}

export interface FeedDto {
  feedID: number;
  feedName: string;
  feedUrl: string;
}

export interface SearchFeedsRequest {
  query: string;
}

export interface RefreshFeedsRequest {

}

export interface LoginRequest {
  password: string;
  username: string;
}

export interface GetFeedItemsResponse {
  feedItems: FeedItemDto[];
  feeds: FeedDto[];
}

export interface FeedItemDto {
  feedName: string;
  feedID: number;
  feedItemAddedTime: Date;
  feedItemDescription: string;
  feedItemID: number;
  feedItemTitle: string;
  feedItemUrl: string;
}

export interface DeleteFeedResponse {
  id: number;
}

export interface NewFeedResponse {
  item: FeedDto;
}

export interface GetFeedResponse {
  item: FeedDto;
}

export interface SaveFeedResponse {
  id: number;
}

export interface SearchFeedsResponse {
  items: FeedDto[];
}

export interface LoginResponse {
  token: string;
  username: string;
}

export interface ServerApiClient {
  getFeedItems: (req: GetFeedItemsRequest) => Promise<Result<GetFeedItemsResponse>>;
  deleteFeed: (req: DeleteFeedRequest) => Promise<Result<DeleteFeedResponse>>;
  newFeed: (req: NewFeedRequest) => Promise<Result<NewFeedResponse>>;
  getFeed: (req: GetFeedRequest) => Promise<Result<GetFeedResponse>>;
  saveFeed: (req: SaveFeedRequest) => Promise<Result<SaveFeedResponse>>;
  searchFeeds: (req: SearchFeedsRequest) => Promise<Result<SearchFeedsResponse>>;
  refreshFeeds: (req: RefreshFeedsRequest) => Promise<Result>;
  login: (req: LoginRequest) => Promise<Result<LoginResponse>>;
}

export const serverApi = (api: ApiClient): ServerApiClient => ({
  getFeedItems: (req) => api.send<GetFeedItemsRequest, GetFeedItemsResponse>('GetFeedItemsRequest', req),
  deleteFeed: (req) => api.send<DeleteFeedRequest, DeleteFeedResponse>('DeleteFeedRequest', req),
  newFeed: (req) => api.send<NewFeedRequest, NewFeedResponse>('NewFeedRequest', req),
  getFeed: (req) => api.send<GetFeedRequest, GetFeedResponse>('GetFeedRequest', req),
  saveFeed: (req) => api.send<SaveFeedRequest, SaveFeedResponse>('SaveFeedRequest', req),
  searchFeeds: (req) => api.send<SearchFeedsRequest, SearchFeedsResponse>('SearchFeedsRequest', req),
  refreshFeeds: (req) => api.send<RefreshFeedsRequest>('RefreshFeedsRequest', req),
  login: (req) => api.send<LoginRequest, LoginResponse>('LoginRequest', req),
});
