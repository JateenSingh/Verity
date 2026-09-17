import { UserSummary } from './user.model';

export type PostTagName = 'MisleadingOrFalse';

export interface PostTag {
  tag: PostTagName;
  taggedBy: UserSummary;
  reason: string | null;
  createdAt: string;
}

export interface PostSummary {
  id: string;
  title: string;
  excerpt: string;
  author: UserSummary;
  createdAt: string;
  likeCount: number;
  commentCount: number;
  tags: PostTag[];
  viewerHasLiked: boolean;
}

export interface PostDetail {
  id: string;
  title: string;
  body: string;
  author: UserSummary;
  createdAt: string;
  likeCount: number;
  commentCount: number;
  tags: PostTag[];
  viewerHasLiked: boolean;
}

export interface CreatePostRequest {
  title: string;
  body: string;
}

export type PostSortBy = 'createdAt' | 'likeCount';
export type SortDirection = 'asc' | 'desc';

export interface PostListQuery {
  page?: number;
  pageSize?: number;
  dateFrom?: string;
  dateTo?: string;
  author?: string;
  authorId?: string;
  tag?: PostTagName;
  untagged?: boolean;
  sortBy?: PostSortBy;
  sortDirection?: SortDirection;
}
