export interface FeedDto {
  feedName: string;
  feedID: number;
  feedUrl: string;
}

export interface NewFeedRequest {
}

export interface NewFeedResponse {
  item: FeedDto;
}

export interface GetFeedRequest {
  id: number;
}

export interface GetFeedResponse {
  item: FeedDto;
}

export interface SearchFeedsRequest {
  query: string;
}

export interface SearchFeedsResponse {
  items: FeedDto[];
}

export interface SaveFeedRequest {
  item: FeedDto;
}

export interface SaveFeedResponse {
  id: number;
}

export interface DeleteFeedRequest {
  id: number;
}

export interface DeleteFeedResponse {
  id: number;
}
