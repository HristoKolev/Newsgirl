export interface GetFeedItemsRequest {

}

export interface DeleteFeedRequest {
  id: number;
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